using Geowerkstatt.Interlis.RepositoryCrawler;
using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler.Test.ToolComparison;

/// <summary>
/// A single compiler-comparison case for one INTERLIS file published on a public model repository. The file under
/// test (<see cref="TargetContent"/>) is compiled together with its imported models (<see cref="Dependencies"/>),
/// which each compiler resolves natively. The repository metadata is written to the test output so a failing model
/// can be traced back to its repository file.
/// </summary>
public sealed record RepositoryModelTestCase
{
    /// <summary>The case display name, derived from the models defined in the repository file.</summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// A failure that prevented any case from being assembled (the repository crawl failed or found no models)
    /// and must fail the run, or <see langword="null"/>.
    /// </summary>
    public string? SetupError { get; init; }

    /// <summary>
    /// A reason this case is not compared, or <see langword="null"/>. Either the crawled repository metadata lists
    /// the model but downloading its file failed (typically a 404 — the repository no longer serves the file), or the
    /// comparison is a known divergence between the two compilers. The case is skipped rather than failed, because
    /// re-running the suite fixes neither and a permanent failure would only add noise; the reason keeps the skip
    /// visible and traceable in the test report.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>The INTERLIS source of the repository file under test.</summary>
    public string? TargetContent { get; init; }

    /// <summary>The transitively imported model files that both compilers may resolve the target's imports against.</summary>
    public IReadOnlyList<ModelFile> Dependencies { get; init; } = [];

    /// <summary>The models defined in the repository file under test, formatted as "Name (Version, SchemaLanguage)".</summary>
    public IReadOnlyList<string> Models { get; init; } = [];

    /// <summary>The URL of the repository file under test.</summary>
    public string? FileUri { get; init; }

    /// <summary>The URLs of the dependency files supplied to the compilers, for traceability in the test output.</summary>
    public IReadOnlyList<string> DependencyFileUris { get; init; } = [];

    public override string ToString() => DisplayName;
}

/// <summary>
/// Discovers all publicly available INTERLIS models with <see cref="RepositorySearcher"/> and produces a
/// <see cref="RepositoryModelTestCase"/> for every published INTERLIS file, so <c>RepositoryModelsComparisonTest</c>
/// can compare the Geowerkstatt compiler against ili2c on real-world models.
/// <para>
/// Each case keeps the file under test and its transitively imported model files separate: the Geowerkstatt compiler
/// resolves the imports through an <see cref="IModelResolver"/> and ili2c through a model directory, so both exercise
/// real import resolution.
/// </para>
/// <see cref="RepositorySearcher"/> caches the crawled repository tree and the downloaded files in a local SQLite
/// database, so the tree is crawled over the network only on the first run and then reused from the cache until it
/// goes stale. The crawled repository and compared schema languages are set by the
/// <see cref="RootUri"/> and <see cref="SchemaLanguages"/> constants below.
/// </summary>
public static class RepositoryModelTestCaseSource
{
    /// <summary>The root repository crawled for the models to compare.</summary>
    private const string RootUri = "https://models.interlis.ch/";

    /// <summary>The schema languages to compare. The Geowerkstatt compiler supports <c>ili2_4</c>.</summary>
    private static readonly string[] SchemaLanguages = ["ili2_4"];

    /// <summary>The internal INTERLIS model is built into both compilers and is never fetched from a repository.</summary>
    private const string InternalInterlisModelName = "INTERLIS";

    /// <summary>The repository files whose comparison is known to diverge, skipped with the reason instead of failing the suite.</summary>
    private static readonly IReadOnlyDictionary<string, string> KnownDivergences = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["https://evd.vd.ch/geo/DGE/VD46.1_PompesChaleur_V1.0.1.ili"] =
            "Known divergence: <Geowerkstatt.Interlis.Compiler> rejects an attribute of type 'OID NUMERIC' ('must be declared ABSTRACT because its type is not fully defined'), which <ili2c-tool> accepts.",
        ["https://evd.vd.ch/geo/DGE/VD102.1_ArbresRemarquables_V1.1.0.ili"] =
            "Known divergence: <Geowerkstatt.Interlis.Compiler> rejects an attribute of type 'OID NUMERIC' ('must be declared ABSTRACT because its type is not fully defined'), which <ili2c-tool> accepts.",
        ["https://evd.vd.ch/geo/DGIP/VD14.1_SitesArcheologiques_V1.0.1.ili"] =
            "Known divergence: <Geowerkstatt.Interlis.Compiler> rejects an attribute of type 'OID NUMERIC' ('must be declared ABSTRACT because its type is not fully defined'), which <ili2c-tool> accepts.",
    };

    private static readonly Lazy<Task<IReadOnlyList<RepositoryModelTestCase>>> LazyCases = new(BuildCasesAsync);

    /// <summary>
    /// The comparison cases for all publicly available INTERLIS models. The repository tree is searched once per
    /// test run on first access (the shared task is awaited by every caller); when the search fails, a single
    /// placeholder case is returned instead so the suite never breaks test discovery.
    /// </summary>
    public static Task<IReadOnlyList<RepositoryModelTestCase>> GetCasesAsync() => LazyCases.Value;

    private static async Task<IReadOnlyList<RepositoryModelTestCase>> BuildCasesAsync()
    {
        try
        {
            return await CrawlCases();
        }
        catch (Exception ex)
        {
            return
            [
                new RepositoryModelTestCase
                {
                    DisplayName = "Repository crawl failed",
                    SetupError = $"Crawling the INTERLIS model repositories failed, no models could be compared: {ex}",
                },
            ];
        }
    }

    private static async Task<IReadOnlyList<RepositoryModelTestCase>> CrawlCases()
    {
        Console.WriteLine($"Searching INTERLIS models from <{RootUri}> for schema language(s) {string.Join(", ", SchemaLanguages)} (using the RepositorySearcher cache)...");

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        using var httpClient = new HttpClient();
        var searcher = CreateSearcher(loggerFactory, httpClient);

        // The searcher crawls the tree (cached) and downloads the matching files (cached), populating Model.FileContent.
        // Dependencies are resolved to models of the same schema language, so the compared-language set already contains
        // every file a case needs; there is no need to fetch the whole tree.
        var searchResult = await searcher.SearchModels(model => SchemaLanguages.Contains(model.SchemaLanguage));

        var allModels = searchResult
            .Where(model => model.Uri is not null && !InternalInterlisModelName.Equals(model.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (allModels.Count == 0)
        {
            return
            [
                new RepositoryModelTestCase
                {
                    DisplayName = "No matching models found",
                    SetupError = $"No models with schema language(s) {string.Join(", ", SchemaLanguages)} were found in the repository tree at <{RootUri}>. The repository may be unreachable or contain no such models.",
                },
            ];
        }

        var resolver = new DependencyResolver(allModels);

        // Group the models by their file URL once (a file can define multiple models); both the file-content lookup
        // and the per-file comparison cases are derived from this single grouping.
        var fileGroups = allModels
            .GroupBy(model => model.Uri!.AbsoluteUri, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToList();

        var fileContents = fileGroups.ToDictionary(
            group => group.Key,
            group => group.Select(model => model.FileContent).FirstOrDefault(file => file is not null),
            StringComparer.Ordinal);

        var repositoryCount = allModels.Select(model => model.ModelRepository).Distinct().Count();
        Console.WriteLine($"Found {allModels.Count} models across {repositoryCount} repositories; comparing {fileGroups.Count} INTERLIS files.");

        var cases = fileGroups
            .Select(group => CreateCase(group.Key, group.ToList(), CollectDependencyClosure(group.Key, resolver), resolver, fileContents))
            .ToList();

        return MakeDisplayNamesUnique(cases);
    }

    /// <summary>
    /// Creates a <see cref="RepositorySearcher"/> for the comparison suite: it crawls <see cref="RootUri"/> and caches
    /// the repository tree and downloaded files in <see cref="CacheDbFolder"/> for <see cref="StaleTime"/>, using the
    /// caller-owned <paramref name="httpClient"/>.
    /// </summary>
    private static RepositorySearcher CreateSearcher(ILoggerFactory loggerFactory, HttpClient httpClient)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RepositoryCrawlerOptions.SectionName}:{nameof(RepositoryCrawlerOptions.RootRepositoryUri)}"] = RootUri,
                [$"{RepositoryCrawlerOptions.SectionName}:{nameof(RepositoryCrawlerOptions.StaleTime)}"] = TimeSpan.FromDays(1).ToString(),
            })
            .Build();

        var crawler = new RepositoryCrawler.RepositoryCrawler(loggerFactory, httpClient);
        return new RepositorySearcher(crawler, configuration, loggerFactory);
    }

    /// <summary>
    /// Collects the URLs of the files transitively imported by the file at <paramref name="targetFileUri"/> (the
    /// target file itself is excluded). Dependencies are resolved by model name through the repository metadata; a
    /// dependency name that resolves to no model with a matching schema language is skipped, so both compilers report
    /// the missing import and still agree. Duplicate definitions and import cycles need no special handling here: the
    /// Geowerkstatt loader de-duplicates models by name and ili2c resolves from the model directory.
    /// </summary>
    private static IReadOnlyList<string> CollectDependencyClosure(string targetFileUri, DependencyResolver resolver)
    {
        var dependencyFiles = new List<string>();
        var visitedFiles = new HashSet<string>(StringComparer.Ordinal) { targetFileUri };
        var stack = new Stack<string>();
        stack.Push(targetFileUri);

        while (stack.Count > 0)
        {
            var fileUri = stack.Pop();
            foreach (var model in resolver.ModelsInFile(fileUri))
            {
                foreach (var dependencyName in model.DependsOnModel.Distinct(StringComparer.Ordinal))
                {
                    if (InternalInterlisModelName.Equals(dependencyName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var dependency = resolver.Resolve(dependencyName, model);
                    if (dependency is null)
                    {
                        continue;
                    }

                    var dependencyUri = dependency.Uri!.AbsoluteUri;
                    if (visitedFiles.Add(dependencyUri))
                    {
                        stack.Push(dependencyUri);
                        dependencyFiles.Add(dependencyUri);
                    }
                }
            }
        }

        dependencyFiles.Sort(StringComparer.Ordinal);
        return dependencyFiles;
    }

    private static RepositoryModelTestCase CreateCase(
        string fileUri,
        IReadOnlyList<Model> targetModels,
        IReadOnlyList<string> dependencyFileUris,
        DependencyResolver resolver,
        IReadOnlyDictionary<string, InterlisFile?> fileContents)
    {
        var testCase = new RepositoryModelTestCase
        {
            DisplayName = string.Join(", ", targetModels.Select(m => string.IsNullOrEmpty(m.Version) ? m.Name : $"{m.Name}[{m.Version}]").Order(StringComparer.Ordinal)),
            FileUri = fileUri,
            SkipReason = KnownDivergences.GetValueOrDefault(fileUri),
            Models = targetModels
                .Select(model => $"{model.Name} ({model.Version}, {model.SchemaLanguage})")
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList(),
            DependencyFileUris = dependencyFileUris,
        };

        var targetFile = fileContents.GetValueOrDefault(fileUri);
        if (targetFile is null)
        {
            return testCase with { SkipReason = $"The model file <{fileUri}> could not be downloaded (the repository lists the model but does not serve the file)." };
        }

        // A dependency file that could not be downloaded is simply omitted; both compilers then report its models as
        // missing imports and still agree.
        var dependencies = dependencyFileUris
            .Select(uri => (Uri: uri, File: fileContents.GetValueOrDefault(uri)))
            .Where(dependency => dependency.File is not null)
            .Select(dependency => new ModelFile(
                resolver.ModelsInFile(dependency.Uri).Select(model => model.Name).Distinct(StringComparer.Ordinal).ToList(),
                dependency.File!.Content,
                dependency.Uri))
            .ToList();

        return testCase with { TargetContent = targetFile.Content, Dependencies = dependencies };
    }

    /// <summary>
    /// Disambiguates duplicate display names (the same model name and version published under multiple file URLs)
    /// by appending the file URL, so every test case remains individually identifiable.
    /// </summary>
    private static IReadOnlyList<RepositoryModelTestCase> MakeDisplayNamesUnique(IReadOnlyList<RepositoryModelTestCase> cases)
        => cases
            .GroupBy(testCase => testCase.DisplayName, StringComparer.Ordinal)
            .SelectMany(group => group.Count() == 1
                ? group
                : group.Select(testCase => testCase with { DisplayName = $"{testCase.DisplayName} ({testCase.FileUri})" }))
            .OrderBy(testCase => testCase.DisplayName, StringComparer.Ordinal)
            .ToList();

    /// <summary>Resolves model names to the published models and files that define them, based on the crawled repository metadata.</summary>
    private sealed class DependencyResolver
    {
        private readonly ILookup<string, Model> modelsByName;
        private readonly ILookup<string, Model> modelsByFileUri;

        public DependencyResolver(IReadOnlyCollection<Model> models)
        {
            modelsByName = models.ToLookup(model => model.Name, StringComparer.Ordinal);
            modelsByFileUri = models.ToLookup(model => model.Uri!.AbsoluteUri, StringComparer.Ordinal);
        }

        /// <summary>All models that the crawled metadata places in the file at <paramref name="fileUri"/>.</summary>
        public IReadOnlyList<Model> ModelsInFile(string fileUri) => modelsByFileUri[fileUri].ToList();

        /// <summary>
        /// Resolves the model that provides the dependency <paramref name="name"/> for <paramref name="importer"/>:
        /// the schema language must match and the latest version wins.
        /// </summary>
        public Model? Resolve(string name, Model importer)
            => modelsByName[name]
                .Where(candidate => candidate.SchemaLanguage.Equals(importer.SchemaLanguage, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(candidate => candidate.Version, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Uri!.AbsoluteUri, StringComparer.Ordinal)
                .FirstOrDefault();
    }
}
