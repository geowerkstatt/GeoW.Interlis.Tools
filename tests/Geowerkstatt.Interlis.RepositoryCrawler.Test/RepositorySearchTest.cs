using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;
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
                Assert.IsNotNull(model.MD5);
                return getCachedFile(model.MD5);
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
}
