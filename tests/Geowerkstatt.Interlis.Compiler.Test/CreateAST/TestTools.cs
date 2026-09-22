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

    /// <summary>
    /// Parses <paramref name="source"/> through the full pipeline and asserts that no diagnostics were reported,
    /// so a test can rely on the returned AST being complete and every reference in it resolved. For tests that
    /// read the AST directly instead of comparing it against an expectation (see <see cref="AssertReadFile"/>).
    /// </summary>
    internal static async Task<InterlisEnvironment> ReadWithoutErrors(string source, string? sourceUri = null)
    {
        var logProvider = new TestLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(logProvider));
        var environment = new InterlisReader(loggerFactory).ReadFile(new StringReader(source), sourceUri);

        await Assert.That(logProvider.GetMessages()).IsEquivalentTo(Array.Empty<string>());
        return environment;
    }

    /// <summary>
    /// Renders a reference as <c>name@range.name@range -&gt; target</c> (<c>-&gt;</c> between the steps of an object
    /// path) — what navigation and rename need from it: each written segment with the span a rename of that name
    /// replaces (as <see cref="RangePosition.ToString"/> renders it, or <c>&lt;none&gt;</c> for an implied name), and
    /// what the whole reference points at. A target that is a definition is shown fully qualified, any other (a
    /// meta-object declaration) by its name, an unresolved one as <c>&lt;unresolved&gt;</c>.
    /// </summary>
    internal static string Describe(IReference reference)
    {
        var separator = reference.Resolution == ReferenceResolution.ObjectPath ? "->" : ".";
        return $"{string.Join(separator, reference.Path.Select(segment => $"{segment}@{segment.Range?.ToString() ?? "<none>"}"))} -> {Describe(reference.Target)}";
    }

    /// <summary>
    /// Renders one written name as <c>segment@range -&gt; target</c>: the segment as written (index or qualifier
    /// included), the span a rename of it replaces, and what it denotes.
    /// </summary>
    internal static string Describe(PathSegment segment)
        => $"{segment}@{segment.Range?.ToString() ?? "<none>"} -> {Describe(segment.Target)}";

    /// <summary>A target as a definition's fully qualified name, another target's (a meta-object declaration's) name, or <c>&lt;unresolved&gt;</c>.</summary>
    private static string Describe(IReferenceTarget? target)
        => target is IInterlisDefinition definition ? definition.FullyQualifiedName : target?.Name ?? "<unresolved>";

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
            .WithCustomComparison(new BareReferenceComparison()) // A reference an expectation writes without a target compares by its path alone
            .IgnoreProperty<IInterlisDefinition>(d => d.Parent) // Ignore parent property to break circular references
            .IgnoreProperty(p => p.DeclaringType.IsGenericType
                    && typeof(Reference<IInterlisDefinition>).GetGenericTypeDefinition() == p.DeclaringType.GetGenericTypeDefinition()
                    && (nameof(Reference<IInterlisDefinition>.Source).Equals(p.Name) // Ignore reference source to break circular references
                        || nameof(Reference<IInterlisDefinition>.MapTarget).Equals(p.Name) // Ignore Func property
                        || nameof(Reference<IInterlisDefinition>.ResolvesInEnvironment).Equals(p.Name) // Ignore resolution plumbing
                        || nameof(Reference<IInterlisDefinition>.Resolution).Equals(p.Name) // Ignore resolution plumbing (asserted by ReferenceRegistrationTest)
                        || nameof(Reference<IInterlisDefinition>.SourceRange).Equals(p.Name))) // Derived from the segments' spans, which are ignored below
            .IgnoreProperty<IInterlisDefinition>(d => d.FullyQualifiedName) // Ignore calculated property
            .IgnoreProperty<ModelDef>(m => m.Dependencies) // Ignore calculated property (derived from Imports and TranslationOf)
            // Ignore definitions' own declaration spans (uniform clutter, like NameLocations); TypeDef spans stay compared
            .IgnoreProperty(p => nameof(ISourceRange.SourceRange).Equals(p.Name)
                    && (typeof(IInterlisDefinition).IsAssignableFrom(p.DeclaringType) || typeof(IExpression).IsAssignableFrom(p.DeclaringType)))
            .IgnoreProperty<IInterlisDefinitionContainer>(d => d.ContainerReferences) // Easy access collection for references
            // A path segment's span and target, like a definition's declaration range: uniform plumbing that would
            // clutter every expected path. The segment names stay compared, and ReferencePathSegmentTest asserts
            // the spans and targets.
            .IgnoreProperty(p => typeof(PathSegment).IsAssignableFrom(p.DeclaringType)
                    && (nameof(PathSegment.Range).Equals(p.Name) || nameof(PathSegment.Target).Equals(p.Name)))
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
    /// Compares a reference an expectation writes without a target — a <c>Path</c> and nothing else — by its path
    /// alone: the segment names and, for an object path, their shapes (<c>[FIRST]</c>, <c>[Assoc]</c>, a keyword).
    /// What the resolver fills in — the target, and the spans and targets of the segments — is uniform plumbing on
    /// every reference that would clutter every expected path; the direct-read tests (<c>ReferencePathSegmentTest</c>,
    /// <c>ObjectPathReferenceTest</c>) assert it. An expectation that does state a target is left to the normal
    /// structural comparison, which compares it.
    /// </summary>
    private sealed class BareReferenceComparison : IComparison
    {
        public bool CanCompare(Type leftType, Type rightType) =>
            leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(Reference<>)
            && rightType.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(Reference<>);

        public (ComparisonResult result, IComparisonContext context) Compare(IComparisonContext context, object leftValue, object rightValue)
        {
            if (leftValue is not IReference { Target: null } expected || rightValue is not IReference actual)
            {
                return (ComparisonResult.Inconclusive, context);
            }

            var expectedPath = string.Join("->", expected.Path);
            var actualPath = string.Join("->", actual.Path);
            return expectedPath == actualPath
                ? (ComparisonResult.Pass, context)
                : (ComparisonResult.Fail, context.AddDifference(expectedPath, actualPath, nameof(IReference.Path)));
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
