using Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;
using RichardSzalay.MockHttp;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Geowerkstatt.Interlis.RepositoryCrawler.Test
{
    [TestClass]
    public class HttpRepositoryReaderTest
    {
        private MockHttpMessageHandler mockHttp;
        private Dictionary<string, MockedRequest> mockRequests;
        private HttpClient mockHttpClient;

        [TestInitialize]
        public void Initialize()
        {
            mockHttp = new MockHttpMessageHandler();
            mockRequests = mockHttp.SetupHttpMockForTestdataFiles();
            mockHttpClient = mockHttp.ToHttpClient();
        }

        [TestMethod]
        public async Task TestReadIliData()
        {
            var reader = new HttpRepositoryReader(new Uri("https://models.geo.admin.testdata"), mockHttpClient);

            var data = await reader.ReadIliData();

            Assert.IsNotNull(data);
            Assert.AreEqual(170, data.Count());
        }

        [TestMethod]
        public async Task TestReadIliSite()
        {
            var reader = new HttpRepositoryReader(new Uri("https://models.geo.admin.testdata"), mockHttpClient);

            var site = await reader.ReadIliSite();

            Assert.IsNotNull(site);
            Assert.IsNotNull(site.parentSites);
            Assert.AreEqual(1, site.parentSites.Length);
            Assert.IsNotNull(site.subsidiarySites);
            Assert.AreEqual(1, site.subsidiarySites.Length);
        }

        [TestMethod]
        public async Task TestReadIliModels()
        {
            var reader = new HttpRepositoryReader(new Uri("https://models.interlis.testdata"), mockHttpClient);

            var models = await reader.ReadIliModels();

            Assert.IsNotNull(models);
            Assert.AreEqual(76, models.Count());
        }

        [TestMethod]
        public async Task TestGetRepositoryFileStream()
        {
            var reader = new HttpRepositoryReader(new Uri("https://models.geo.admin.testdata/"), mockHttpClient);

            await using var stream = await reader.GetRepositoryFileStream("ARE/SectoralPlans_Catalogues_V1_4.xml");
            Assert.IsNotNull(stream);
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.Length > 0);

            using var sr = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var firstLine = await sr.ReadLineAsync();
            var secondLine = await sr.ReadLineAsync();

            var expectedFirstLine = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            var expectedSecondLine = "<!-- File SectoralPlans_Catalogues_V1_4.xml 2016-11-07 (http://models.geo.admin.ch/ARE) -->";

            Assert.AreEqual(expectedFirstLine, firstLine);
            Assert.AreEqual(expectedSecondLine, secondLine);
        }

        [TestMethod]
        public async Task TestGetRepositoryFileStreamWithDifferentPaths()
        {
            var readerWithoutTrailing = new HttpRepositoryReader(new Uri("https://models.geo.admin.testdata"), mockHttpClient);
            var readerWithTrailing = new HttpRepositoryReader(new Uri("https://models.geo.admin.testdata/"), mockHttpClient);

            await using var stream0 = await readerWithoutTrailing.GetRepositoryFileStream("ARE/SectoralPlans_Catalogues_V1_4.xml");
            await using var stream1 = await readerWithoutTrailing.GetRepositoryFileStream("/ARE/SectoralPlans_Catalogues_V1_4.xml");
            await using var stream2 = await readerWithTrailing.GetRepositoryFileStream("ARE/SectoralPlans_Catalogues_V1_4.xml");
            await using var stream3 = await readerWithTrailing.GetRepositoryFileStream("/ARE/SectoralPlans_Catalogues_V1_4.xml");
        }

        [TestMethod]
        public async Task TestGetRepositoryFileStreamKeepsRepositorySubPath()
        {
            // Repositories are published with and without a trailing slash (<https://405.sia.ch/models>); both serve
            // their files from the repository path, not from the host root.
            var reader = new HttpRepositoryReader(new Uri("https://models.geo.admin.testdata/ARE"), mockHttpClient);

            await using var stream = await reader.GetRepositoryFileStream("SectoralPlans_Catalogues_V1_4.xml");

            Assert.IsNotNull(stream);
            Assert.AreEqual(1, mockHttp.GetMatchCount(mockRequests["https://models.geo.admin.testdata/ARE/SectoralPlans_Catalogues_V1_4.xml"]));
        }

        [TestMethod]
        public async Task TestGetRepositoryFileStreamHandlesHttpErrors()
        {
            var reader = new HttpRepositoryReader(new Uri("https://models.geo.admin.testdata/"), mockHttpClient);
            var ex = await Assert.ThrowsExceptionAsync<RepositoryReaderException>(async () =>
            {
                await using var stream = await reader.GetRepositoryFileStream("NonExistingFile.xml");
            });
            Assert.AreEqual("Error reading file <NonExistingFile.xml> from HTTP repository <https://models.geo.admin.testdata/>", ex.Message);
            Assert.IsInstanceOfType(ex.InnerException, typeof(HttpRequestException));
        }
    }
}
