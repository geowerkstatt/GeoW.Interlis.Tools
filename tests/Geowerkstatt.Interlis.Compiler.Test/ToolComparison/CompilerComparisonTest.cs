using System.Reflection;
using TUnit.Core.Interfaces;

namespace Geowerkstatt.Interlis.Compiler.Test.ToolComparison;

public class CompilerComparisonTest
{
    private const string FullTestCasesMethodName = "GetFullTestCases";

    private static CompilerComparison comparison = default!;

    [Before(Class)]
    public static void Initialize()
    {
        comparison = new CompilerComparison();
    }

    [After(Class)]
    public static void Cleanup()
    {
        comparison.Dispose();
    }

    public sealed class ProcessorCountLimit : IParallelLimit
    {
        public int Limit => Environment.ProcessorCount;
    }

    /// <summary>
    /// Aggregates the <c>GetFullTestCases</c> of every test class in the test assembly. New test classes
    /// are discovered automatically, so a class can never be silently left out of the compiler comparison
    /// (the previous hand-maintained list of per-class <c>[MethodDataSource]</c> attributes made that easy).
    /// </summary>
    public static IEnumerable<TestDataRow<CompilationTestCase>> GetComparisonCases()
    {
        foreach (var source in ComparisonCaseSources())
        {
            foreach (var testCase in (IEnumerable<TestDataRow<CompilationTestCase>>)source.Invoke(null, null)!)
            {
                yield return testCase;
            }
        }
    }

    [Test]
    [ParallelLimiter<ProcessorCountLimit>]
    [MethodDataSource(nameof(GetComparisonCases))]
    public async Task CompareCompilers(CompilationTestCase data)
    {
        TestTools.WriteTestCaseInfo(data);
        await comparison.CompareCompilersAsync(data.Input, data.Ili2cDivergenceReason);
    }

    /// <summary>
    /// Guards the convention behind <see cref="GetComparisonCases"/>: every class that produces compilation
    /// test cases must expose a discoverable <c>GetFullTestCases</c>, otherwise its cases would never reach
    /// the comparison. Fails fast when a new or renamed test class breaks the convention.
    /// </summary>
    [Test]
    public async Task EveryTestCaseClassFeedsComparison()
    {
        var unreachable = string.Join(", ", typeof(CompilerComparisonTest).Assembly.GetTypes()
            .Where(type => type != typeof(CompilerComparisonTest)
                && ProducesCompilationCases(type)
                && FullTestCasesMethod(type) is null)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal));

        using (Assert.Multiple())
        {
            await Assert.That(GetComparisonCases().Any()).IsTrue();
            await Assert.That(unreachable).IsEmpty();
        }
    }

    private static IEnumerable<MethodInfo> ComparisonCaseSources() =>
        typeof(CompilerComparisonTest).Assembly.GetTypes()
            .Select(FullTestCasesMethod)
            .OfType<MethodInfo>()
            .OrderBy(method => method.DeclaringType!.Name, StringComparer.Ordinal);

    private static MethodInfo? FullTestCasesMethod(Type type)
    {
        var method = type.GetMethod(FullTestCasesMethodName, BindingFlags.Public | BindingFlags.Static, binder: null, Type.EmptyTypes, modifiers: null);
        return method is not null && ReturnsCompilationCases(method) ? method : null;
    }

    private static bool ProducesCompilationCases(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Any(method => method.GetParameters().Length == 0 && ReturnsCompilationCases(method));

    private static bool ReturnsCompilationCases(MethodInfo method) =>
        typeof(IEnumerable<TestDataRow<CompilationTestCase>>).IsAssignableFrom(method.ReturnType);
}
