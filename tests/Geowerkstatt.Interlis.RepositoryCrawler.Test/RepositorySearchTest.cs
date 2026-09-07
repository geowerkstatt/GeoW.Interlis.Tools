using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

[TestClass]
public class RepositorySearchTest
{
    private RepositorySearcher repositorySearch;
    private MockHttpMessageHandler mockHttp;
    private ILoggerFactory loggerFactory;
    private IConfigurationRoot configuration;

    [TestInitialize]
    public void TestInitialize()
    {
        mockHttp = new MockHttpMessageHandler();
        mockHttp.SetupHttpMockForTestdataFiles();
        var httpClient = mockHttp.ToHttpClient();

        var loggerProvider = new MockLoggerProvider();
        loggerFactory = LoggerFactory.Create(b => b.AddConsole().AddProvider(loggerProvider));

        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"RepositoryCrawler:RootRepositoryUri", "https://models.interlis.testdata/"},
                {"RepositoryCrawler:CacheDbFolder", Path.Combine(Path.GetTempPath(), "Geowerkstatt.Interlis.Test")},
            })
            .Build();

        var repositoryCrawler = new RepositoryCrawler(loggerFactory, httpClient);
        repositorySearch = new RepositorySearcher(repositoryCrawler, configuration, loggerFactory);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        mockHttp.Dispose();
        await repositorySearch.DeleteCacheDatabase();
    }

    [TestMethod]
    public async Task SearchModel()
    {
        var model = await repositorySearch.SearchModel("TwoModelsInOneFile_Model1");
        Assert.AreEqual("TwoModelsInOneFile_Model1", model?.Name);
        Assert.IsNotNull(model?.FileContent);
    }

    [TestMethod]
    public async Task SearchModelCaseSensitivity()
    {
        var model = await repositorySearch.SearchModel("TWOMODELSINONEFILE_model1");
        Assert.AreEqual(null, model, "Model names are case sensitive.");
    }

    [TestMethod]
    public async Task SearchModelsWithPredicate()
    {
        var models = await repositorySearch.SearchModels(m => m.SchemaLanguage == "ili2_4");
        models.AssertItems(_ => true, m => Assert.AreEqual("ili2_4", m.SchemaLanguage), 35);
    }

    [TestMethod]
    public async Task SearchModelsFromCache()
    {
        // Populate the cache with the repository crawler
        var models = await repositorySearch.SearchModels(m => m.SchemaLanguage == "ili2_4");
        models.AssertItems(_ => true, m => Assert.AreEqual("ili2_4", m.SchemaLanguage), 35);

        // New instance reuses the cache
        var crawler = new Mock<IRepositoryCrawler>(MockBehavior.Strict);
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .ReturnsAsync((Model model, Func<string, InterlisFile?> getCachedFile) =>
            {
                var url = model.Uri?.AbsoluteUri;
                Assert.IsNotNull(url);
                return getCachedFile(url);
            });
        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);

        models = await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");
        models.AssertItems(_ => true, m => Assert.AreEqual("ili2_4", m.SchemaLanguage), 35);

        crawler.VerifyAll();
    }

    [TestMethod]
    public async Task SearchModelsWithWrongHash()
    {
        var models = await repositorySearch.SearchModels(m => m.SchemaLanguage == "ili2_3" && m.ModelRepository != null && m.ModelRepository.HostNameId == "https://models.multiparent.testdata/");
        models.AssertItems(_ => true, m => Assert.AreEqual("ili2_3", m.SchemaLanguage), 5);
    }

    [TestMethod]
    public async Task SearchModelWithWrongHashMultipleTimes()
    {
        const string modelName = "Test_Model_With_Wrong_MD5";

        // Add the model with a wrong hash in ilimodels.xml to the cache
        var uncachedModel = await repositorySearch.SearchModel(modelName);
        Assert.IsNotNull(uncachedModel?.FileContent);

        // Search again, file is already in cache with the correct hash
        var cachedModel = await repositorySearch.SearchModel(modelName);
        Assert.IsNotNull(cachedModel?.FileContent);

        Assert.AreEqual(uncachedModel?.FileContent?.MD5, cachedModel?.FileContent?.MD5);
    }

    [TestMethod]
    public async Task UpdateRepositoryTreePrunesCacheForRemovedModels()
    {
        // Force a re-crawl on every search, so the prune runs each time.
        configuration["RepositoryCrawler:StaleTime"] = "00:00:00";
        var cacheDbFolder = configuration["RepositoryCrawler:CacheDbFolder"]!;

        var model = new Model { Name = "PruneModel", SchemaLanguage = "ili2_4", File = "PruneModel.ili", Version = "1", MD5 = "HASH" };
        var repository = new Repository { HostNameId = "https://prune.testdata/", Uri = new Uri("https://prune.testdata/"), Name = "prune", Models = new HashSet<Model> { model } };
        model.ModelRepository = repository;

        // The first crawl serves the model; the second no longer contains it (the model was removed from the repository).
        var crawls = new Queue<IDictionary<string, Repository>>(
        [
            new Dictionary<string, Repository> { { repository.HostNameId, repository } },
            new Dictionary<string, Repository>(),
        ]);
        var crawler = new Mock<IRepositoryCrawler>();
        crawler.Setup(c => c.CrawlModelRepositories(It.IsAny<RepositoryCrawlerOptions>())).ReturnsAsync(() => crawls.Dequeue());
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .ReturnsAsync(() => new InterlisFile { MD5 = "HASH", Content = "content" });

        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);

        // First search caches the model's file.
        await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");
        using (var afterFirst = OpenCacheContext(cacheDbFolder))
        {
            Assert.AreEqual(1, afterFirst.InterlisFiles.Count());
            Assert.AreEqual(1, afterFirst.InterlisFileReferences.Count());
        }

        // Second search re-crawls an empty tree; the now-stale reference and its orphaned file must be pruned.
        await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");
        using var afterSecond = OpenCacheContext(cacheDbFolder);
        Assert.AreEqual(0, afterSecond.InterlisFileReferences.Count(), "The reference for the removed model's URL should be pruned.");
        Assert.AreEqual(0, afterSecond.InterlisFiles.Count(), "The orphaned file should be pruned.");
    }

    private static RepositoryCrawlerContext OpenCacheContext(string cacheDbFolder)
    {
        var dbFile = Directory.GetFiles(cacheDbFolder, "*.db").Single();
        var options = new DbContextOptionsBuilder<RepositoryCrawlerContext>().UseSqlite($"Data Source={dbFile}").Options;
        return new RepositoryCrawlerContext(options);
    }
}
