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
    private Mock<ILogger> logger;

    [TestInitialize]
    public void TestInitialize()
    {
        mockHttp = new MockHttpMessageHandler();
        mockHttp.SetupHttpMockForTestdataFiles();
        var httpClient = mockHttp.ToHttpClient();

        var loggerProvider = new MockLoggerProvider();
        logger = loggerProvider.LoggerMock;
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().AddProvider(loggerProvider));

        var configuration = new ConfigurationBuilder()
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
    public void TestCleanup()
    {
        mockHttp.Dispose();
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
}
