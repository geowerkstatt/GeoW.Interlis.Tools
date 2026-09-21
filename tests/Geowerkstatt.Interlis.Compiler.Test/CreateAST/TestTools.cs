using DeepEqual;
using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.CreateAST;
using Geowerkstatt.Interlis.Compiler.Test;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Logging;

namespace Geowerkstatt.Interlis.Compiler;

public class TestTools
{
    /// <summary>
    /// Invokes <paramref name="build"/> and returns its result. Lets an expression be built with
    /// multiple statements in place (e.g. a test case <c>Expected</c> value) without leaking the
    /// intermediate local variables into the enclosing method scope.
    /// </summary>
    internal static T Build<T>(Func<T> build) => build();

    internal static async Task AssertReadFile(CompilationTestCase data)
    {
        WriteTestCaseInfo(data);

        var logProvider = new TestLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().AddProvider(logProvider));
        var actual = new InterlisReader(loggerFactory).ReadFile(new StringReader(data.Input));

        using (Assert.Multiple())
        {
            if (data.AssertOutput)
            {
                await AssertDeepEqual(data.Expected, actual, deepEqual => deepEqual
                        .IgnoreProperty<IInterlisDefinition>(p => p.NameLocations) // Ignore NameLocations because it adds too much clutter in tests for whole interlis files
                        .IgnoreProperty<ISourceRange>(p => p.SourceRange)
                        );
            }

            await Assert.That(logProvider.GetMessages()).IsEquivalentTo(data.ExpectedLog ?? [], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        }
    }

    internal static async Task AssertReadRule<TResult>(CompilationTestCase data, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule)
    {
        WriteTestCaseInfo(data);

        var testLogger = new TestLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().AddProvider(testLogger));
        var actual = new InterlisReader(loggerFactory).ReadRule(new StringReader(data.Input), parseRule);

        using (Assert.Multiple())
        {
            if (data.AssertOutput)
            {
                await AssertDeepEqual(data.Expected, actual);
            }

            await Assert.That(testLogger.GetMessages()).IsEquivalentTo(data.ExpectedLog ?? [], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        }
    }

    /// <summary>
    /// Writes the reference handbook (and eCH-0117) links of the given <paramref name="data"/>, its description and
    /// the INTERLIS input to the test output.
    /// </summary>
    internal static void WriteTestCaseInfo(CompilationTestCase data)
    {
        var wroteAnything = false;

        if (data.RefHB is not null)
        {
            TestContext.Current!.Output.WriteLine($"Referenzhandbuch: {ReferenceDocument.RefHB.Link(data.RefHB)}");
            wroteAnything = true;
        }

        if (data.Ech0117 is not null)
        {
            TestContext.Current!.Output.WriteLine($"eCH-0117 Meta-Attribute: {ReferenceDocument.Ech0117.Link(data.Ech0117)}");
            wroteAnything = true;
        }

        if (wroteAnything)
        {
            TestContext.Current!.Output.WriteLine(string.Empty);
        }

        if (data.Description is not null)
        {
            TestContext.Current!.Output.WriteLine(data.Description);
            TestContext.Current!.Output.WriteLine(string.Empty);
        }

        TestContext.Current!.Output.WriteLine(data.Input);
        TestContext.Current!.Output.WriteLine(string.Empty);
    }

    private static async Task AssertDeepEqual(object? expected, object? actual, Func<CompareSyntax<object, object?>, CompareSyntax<object, object?>>? configureDeepEqual = null)
    {
        // Assert the runtime type explicitly because DeepEqual compares structurally and would not catch a type mismatch with matching property values.
        await Assert.That(actual?.GetType()).IsEqualTo<Type>(expected?.GetType());

        if (expected is null)
        {
            return;
        }

        var deepEqualAssert = expected.WithDeepEqual(actual)
            .WithCustomComparison(new AstNodeTypeComparison()) // Nested AST nodes must also match by runtime type, not just structurally
            .IgnoreProperty<IInterlisDefinition>(d => d.Parent) // Ignore parent property to break circular references
            .IgnoreProperty(p => p.DeclaringType.IsGenericType
                    && typeof(Reference<IInterlisDefinition>).GetGenericTypeDefinition() == p.DeclaringType.GetGenericTypeDefinition()
                    && (nameof(Reference<IInterlisDefinition>.Source).Equals(p.Name) // Ignore reference source to break circular references
                        || nameof(Reference<IInterlisDefinition>.MapTarget).Equals(p.Name) // Ignore Func property
                        || nameof(Reference<IInterlisDefinition>.ResolvesInEnvironment).Equals(p.Name))) // Ignore resolution plumbing
            .IgnoreProperty<IInterlisDefinition>(d => d.FullyQualifiedName) // Ignore calculated property
            // Ignore definitions' own declaration spans (uniform clutter, like NameLocations); TypeDef spans stay compared
            .IgnoreProperty(p => nameof(ISourceRange.SourceRange).Equals(p.Name)
                    && (typeof(IInterlisDefinition).IsAssignableFrom(p.DeclaringType) || typeof(IExpression).IsAssignableFrom(p.DeclaringType)))
            .IgnoreProperty<IInterlisDefinitionContainer>(d => d.ContainerReferences) // Easy access collection for references
            .IgnoreCircularReferences();

        if (configureDeepEqual != null)
        {
            deepEqualAssert = configureDeepEqual(deepEqualAssert);
        }

        try
        {
            deepEqualAssert.Assert();
        }
        catch (DeepEqualException ex)
        {
            // Route the failure through Assert.Fail so it is collected by an enclosing Assert.Multiple scope instead of throwing immediately.
            Assert.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Fails the comparison when two AST nodes have different runtime types. DeepEqual compares complex objects
    /// structurally (property by property), so two node types with the same property set — e.g.
    /// <c>ObjectType</c> vs <c>UnresolvedNamedType</c>, or two property-less <c>TypeDef</c>s or unary
    /// expressions — would otherwise compare equal. Applies only when the types differ and at least one side is a
    /// compiler AST type; equal runtime types fall through to the normal structural comparison.
    /// </summary>
    private sealed class AstNodeTypeComparison : IComparison
    {
        public bool CanCompare(Type leftType, Type rightType) =>
            leftType != rightType && (IsAstType(leftType) || IsAstType(rightType));

        public (ComparisonResult result, IComparisonContext context) Compare(IComparisonContext context, object leftValue, object rightValue) =>
            (ComparisonResult.Fail, context.AddDifference(leftValue.GetType(), rightValue.GetType(), "GetType()"));

        private static readonly string AstNamespace = typeof(IInterlisDefinition).Namespace!;

        private static bool IsAstType(Type type) =>
            type.Namespace == AstNamespace || type.Namespace?.StartsWith(AstNamespace + ".", StringComparison.Ordinal) == true;
    }
}
