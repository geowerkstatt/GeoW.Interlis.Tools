namespace Geowerkstatt.Interlis.Compiler.Test.ToolComparison;

/// <summary>
/// Compares the Geowerkstatt compiler against ili2c on every publicly available INTERLIS model, discovered by
/// crawling the public model repository tree (see <see cref="RepositoryModelTestCaseSource"/> for how the cases are
/// assembled and configured). The crawled repository is cached, so after the first run the models are reused from the
/// cache rather than re-downloaded.
/// </summary>
public class RepositoryModelsComparisonTest
{
    private static CompilerComparison comparison = default!;

    [Before(Class)]
    public static void Initialize()
    {
        comparison = new CompilerComparison();
    }

    [After(Class)]
    public static void Cleanup()
    {
        comparison?.Dispose();
    }

    public static async IAsyncEnumerable<TestDataRow<RepositoryModelTestCase>> GetRepositoryModelCases()
    {
        foreach (var testCase in await RepositoryModelTestCaseSource.GetCasesAsync())
        {
            yield return new TestDataRow<RepositoryModelTestCase>(testCase, DisplayName: testCase.DisplayName, Categories: ["RepositoryComparison"]);
        }
    }

    [Test]
    [ParallelLimiter<CompilerComparisonTest.ProcessorCountLimit>]
    [MethodDataSource(nameof(GetRepositoryModelCases))]
    public async Task CompareCompilers(RepositoryModelTestCase data)
    {
        WriteTestCaseInfo(data);

        if (data.SkipReason is not null)
        {
            Skip.Test(data.SkipReason);
        }

        if (data.SetupError is not null)
        {
            Assert.Fail(data.SetupError);
        }

        await comparison.CompareCompilersAsync(data.TargetContent!, data.Dependencies);
    }

    /// <summary>Writes the repository metadata of the given <paramref name="data"/> to the test output, so a failing model can be traced back to its repository file.</summary>
    private static void WriteTestCaseInfo(RepositoryModelTestCase data)
    {
        var output = TestContext.Current!.Output;

        if (data.FileUri is not null)
        {
            output.WriteLine($"Repository file: {data.FileUri}");
        }

        if (data.Models.Count > 0)
        {
            output.WriteLine($"Models: {string.Join(", ", data.Models)}");
        }

        if (data.DependencyFileUris.Count > 0)
        {
            output.WriteLine("Imported dependency files supplied to both compilers:");
            foreach (var dependencyFile in data.DependencyFileUris)
            {
                output.WriteLine($"  {dependencyFile}");
            }
        }

        output.WriteLine(string.Empty);
    }
}
