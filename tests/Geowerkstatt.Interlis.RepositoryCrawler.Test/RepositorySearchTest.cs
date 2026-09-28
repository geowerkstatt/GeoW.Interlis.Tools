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

    [TestMethod]
    public async Task ConcurrentSearchesOnAStaleCacheCrawlOnce()
    {
        var crawlStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var crawlMayFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var crawls = 0;

        var crawler = new Mock<IRepositoryCrawler>();
        crawler
            .Setup(c => c.CrawlModelRepositories(It.IsAny<RepositoryCrawlerOptions>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref crawls);
                crawlStarted.TrySetResult();
                await crawlMayFinish.Task;
                return SingleModelTree();
            });
        // No file is served, so the searches have nothing to write besides the tree.
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .ReturnsAsync((InterlisFile?)null);

        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);
        var first = searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");
        await crawlStarted.Task;

        // Started while the first search is still crawling: from the same searcher and from another one on the
        // same cache, as a process may create several. Both find the tree stale and must wait for the running crawl
        // instead of starting their own.
        var second = searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");
        var third = new RepositorySearcher(crawler.Object, configuration, loggerFactory).SearchModels(m => m.SchemaLanguage == "ili2_4");
        crawlMayFinish.SetResult();

        var results = await Task.WhenAll(first, second, third);

        Assert.AreEqual(1, crawls, "the repository tree is crawled once");
        foreach (var models in results)
        {
            Assert.AreEqual(1, models.Count, "every search sees the crawled tree");
            Assert.AreEqual("ConcurrentModel", models[0].Name);
        }
    }

    [TestMethod]
    public async Task KeepsATreeRefreshedByAnotherProcessWhileCrawling()
    {
        var cacheDbFolder = configuration["RepositoryCrawler:CacheDbFolder"]!;

        var crawler = new Mock<IRepositoryCrawler>();
        crawler
            .Setup(c => c.CrawlModelRepositories(It.IsAny<RepositoryCrawlerOptions>()))
            .ReturnsAsync(() =>
            {
                // While this crawl runs, another process (simulated by a separate connection) refreshes the cache.
                // Its crawl time was assigned before its commit, so it can lie before this search's staleness check.
                using var otherProcess = OpenCacheContext(cacheDbFolder);
                otherProcess.Repositories.AddRange(SingleModelTree("OtherProcessModel").Values);
                otherProcess.CrawlInformations.Add(new CrawlInformation { CrawlTime = DateTime.Now.AddMinutes(-1) });
                otherProcess.SaveChanges();

                return SingleModelTree("ConcurrentModel");
            });
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .ReturnsAsync((InterlisFile?)null);

        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);
        var models = await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");

        Assert.AreEqual(1, models.Count);
        Assert.AreEqual("OtherProcessModel", models[0].Name, "the newer tree of the other process is kept, this crawl's result is dropped");
    }

    [TestMethod]
    public async Task ConcurrentSearchesCacheTheSameFileOnce()
    {
        var cacheDbFolder = configuration["RepositoryCrawler:CacheDbFolder"]!;
        var fetches = 0;
        var bothFetching = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var crawler = new Mock<IRepositoryCrawler>();
        crawler.Setup(c => c.CrawlModelRepositories(It.IsAny<RepositoryCrawlerOptions>())).ReturnsAsync(() => SingleModelTree());
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .Returns(async (Model model, Func<string, InterlisFile?> _) =>
            {
                // Both searches fetch the file before either of them saved it, as when two documents importing the
                // same model are opened at once.
                if (Interlocked.Increment(ref fetches) == 2)
                {
                    bothFetching.SetResult();
                }

                await bothFetching.Task;
                return model.FileContent = new InterlisFile { MD5 = "HASH", Content = "content" };
            });

        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);
        await searcher.SearchModels(m => m.Name == "none");   // Crawls the tree, so both searches below start with a fresh cache.

        var results = await Task.WhenAll(
            searcher.SearchModels(m => m.SchemaLanguage == "ili2_4"),
            searcher.SearchModels(m => m.SchemaLanguage == "ili2_4"));

        foreach (var models in results)
        {
            Assert.AreEqual("content", models.Single().FileContent?.Content);
        }

        using var cache = OpenCacheContext(cacheDbFolder);
        Assert.AreEqual(1, cache.InterlisFiles.Count(), "the file is stored once");
        Assert.AreEqual(1, cache.InterlisFileReferences.Count(), "the URL points at it once");
    }

    [TestMethod]
    public async Task ReconcilesTheFileCacheWithAnotherProcess()
    {
        var cacheDbFolder = configuration["RepositoryCrawler:CacheDbFolder"]!;
        var url = "https://concurrent.testdata/ConcurrentModel.ili";

        var crawler = new Mock<IRepositoryCrawler>();
        crawler.Setup(c => c.CrawlModelRepositories(It.IsAny<RepositoryCrawlerOptions>())).ReturnsAsync(() => SingleModelTree());
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .ReturnsAsync((Model model, Func<string, InterlisFile?> _) =>
            {
                // While this fetch runs, another process (simulated by a separate connection) caches the same URL
                // with an older content of the file.
                using var otherProcess = OpenCacheContext(cacheDbFolder);
                otherProcess.InterlisFiles.Add(new InterlisFile { MD5 = "OLD", Content = "old content" });
                otherProcess.InterlisFileReferences.Add(new InterlisFileReference { SourceUrl = url, MD5 = "OLD" });
                otherProcess.SaveChanges();

                return model.FileContent = new InterlisFile { MD5 = "NEW", Content = "new content" };
            });

        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);
        var models = await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");

        Assert.AreEqual("new content", models.Single().FileContent?.Content);
        using var cache = OpenCacheContext(cacheDbFolder);
        Assert.AreEqual("NEW", cache.InterlisFileReferences.Single(reference => reference.SourceUrl == url).MD5, "the URL points at the content fetched here, which is newer");
        CollectionAssert.AreEquivalent(new[] { "NEW", "OLD" }, cache.InterlisFiles.Select(file => file.MD5).ToList(), "both contents are stored; the unreferenced one goes with the next prune");
    }

    [TestMethod]
    public async Task ReinsertsAReferencePrunedByAnotherProcess()
    {
        var cacheDbFolder = configuration["RepositoryCrawler:CacheDbFolder"]!;
        var url = "https://concurrent.testdata/ConcurrentModel.ili";
        var fetchedContent = new Queue<string>(["HASH", "HASH2"]);

        var crawler = new Mock<IRepositoryCrawler>();
        crawler.Setup(c => c.CrawlModelRepositories(It.IsAny<RepositoryCrawlerOptions>())).ReturnsAsync(() => SingleModelTree());
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .ReturnsAsync((Model model, Func<string, InterlisFile?> _) =>
            {
                var hash = fetchedContent.Dequeue();
                if (hash == "HASH2")
                {
                    // While the second fetch runs, another process prunes the reference the first search stored.
                    using var otherProcess = OpenCacheContext(cacheDbFolder);
                    otherProcess.InterlisFileReferences.Where(reference => reference.SourceUrl == url).ExecuteDelete();
                }

                return model.FileContent = new InterlisFile { MD5 = hash, Content = hash };
            });

        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);
        await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");
        // The file changed on the server: the second search has to repoint the reference, whose row is gone by then.
        var models = await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");

        Assert.AreEqual("HASH2", models.Single().FileContent?.Content);
        using var cache = OpenCacheContext(cacheDbFolder);
        Assert.AreEqual("HASH2", cache.InterlisFileReferences.Single(reference => reference.SourceUrl == url).MD5);
    }

    [TestMethod]
    public async Task DoesNotReinsertAModelReplacedByAnotherProcess()
    {
        var cacheDbFolder = configuration["RepositoryCrawler:CacheDbFolder"]!;

        // The catalog declares no hash for the model, so the crawler adopts the fetched file's hash on the tracked
        // model, which the search then saves.
        var model = new Model { Name = "NoHash", SchemaLanguage = "ili2_4", File = "NoHash.ili", Version = "1", MD5 = null };
        var repository = new Repository { HostNameId = "https://concurrent.testdata/", Uri = new Uri("https://concurrent.testdata/"), Name = "concurrent", Models = new HashSet<Model> { model } };
        model.ModelRepository = repository;

        var crawler = new Mock<IRepositoryCrawler>();
        crawler.Setup(c => c.CrawlModelRepositories(It.IsAny<RepositoryCrawlerOptions>())).ReturnsAsync(() => new Dictionary<string, Repository> { { repository.HostNameId, repository } });
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .ReturnsAsync((Model fetched, Func<string, InterlisFile?> _) =>
            {
                // While the fetch runs, another process refreshes the tree: the model row is replaced by a newer one.
                using var otherProcess = OpenCacheContext(cacheDbFolder);
                otherProcess.Models.ExecuteDelete();
                otherProcess.Models.Add(new Model { Name = "NoHash", SchemaLanguage = "ili2_4", File = "NoHash.ili", Version = "2", MD5 = "CATALOG", ModelRepository = otherProcess.Repositories.Single() });
                otherProcess.SaveChanges();

                fetched.MD5 = "ADOPTED";
                return fetched.FileContent = new InterlisFile { MD5 = "ADOPTED", Content = "content" };
            });

        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);
        var models = await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");

        Assert.AreEqual("content", models.Single().FileContent?.Content, "the search still has its file");
        using var cache = OpenCacheContext(cacheDbFolder);
        Assert.AreEqual("2", cache.Models.Single().Version, "only the other process's row is left; the adopted hash of the replaced row is not inserted next to it");
        Assert.AreEqual(1, cache.InterlisFileReferences.Count(), "the file itself is cached");
    }

    [TestMethod]
    public async Task ATreeThatCannotBeStoredFailsTheRefreshButNotTheSearch()
    {
        var cacheDbFolder = configuration["RepositoryCrawler:CacheDbFolder"]!;

        // A crawled model without a name violates the NOT NULL constraint when the tree is stored.
        var tree = SingleModelTree();
        tree.Values.Single().Models.Single().Name = null!;
        var crawler = new Mock<IRepositoryCrawler>();
        crawler.Setup(c => c.CrawlModelRepositories(It.IsAny<RepositoryCrawlerOptions>())).ReturnsAsync(() => tree);
        crawler
            .Setup(c => c.FetchInterlisFile(It.IsAny<Model>(), It.IsAny<Func<string, InterlisFile?>>()))
            .ReturnsAsync((InterlisFile?)null);

        var searcher = new RepositorySearcher(crawler.Object, configuration, loggerFactory);
        var models = await searcher.SearchModels(m => m.SchemaLanguage == "ili2_4");

        Assert.AreEqual(0, models.Count, "the search answers from the tree that is there, which is empty");
        using var cache = OpenCacheContext(cacheDbFolder);
        Assert.AreEqual(0, cache.Repositories.Count(), "nothing of the failed tree was stored, neither by the refresh nor by the search's save");
    }

    /// <summary>A repository tree with one INTERLIS 2.4 model of the given name, as a crawl would return it.</summary>
    private static IDictionary<string, Repository> SingleModelTree(string modelName = "ConcurrentModel")
    {
        var model = new Model { Name = modelName, SchemaLanguage = "ili2_4", File = $"{modelName}.ili", Version = "1", MD5 = "HASH" };
        var repository = new Repository { HostNameId = "https://concurrent.testdata/", Uri = new Uri("https://concurrent.testdata/"), Name = "concurrent", Models = new HashSet<Model> { model } };
        model.ModelRepository = repository;
        return new Dictionary<string, Repository> { { repository.HostNameId, repository } };
    }

    private static RepositoryCrawlerContext OpenCacheContext(string cacheDbFolder)
    {
        var dbFile = Directory.GetFiles(cacheDbFolder, "*.db").Single();
        var options = new DbContextOptionsBuilder<RepositoryCrawlerContext>().UseSqlite($"Data Source={dbFile}").Options;
        return new RepositoryCrawlerContext(options);
    }
}
