using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;
using System.Text;

namespace Geowerkstatt.Interlis.RepositoryCrawler.Test
{
    [TestClass]
    public class LocalRepositoryReaderTest
    {
        [TestMethod]
        public async Task TestReadIliData()
        {
            var repositoryDir = "./Testdata/models.geo.admin.testdata";
            var reader = new LocalRepositoryReader(repositoryDir);

            var iliData = await reader.ReadIliData();

            Assert.IsNotNull(iliData);
            Assert.AreEqual(170, iliData.Count());
        }

        [TestMethod]
        public async Task TestReadIliSite()
        {
            var repositoryDir = "./Testdata/models.geo.admin.testdata";
            var reader = new LocalRepositoryReader(repositoryDir);

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
            var repositoryDir = "./Testdata/models.interlis.testdata";
            var reader = new LocalRepositoryReader(repositoryDir);

            var models = await reader.ReadIliModels();

            Assert.IsNotNull(models);
            Assert.AreEqual(76, models.Count());
        }

        [TestMethod]
        public async Task TestGetRepositoryFileStream()
        {
            var repositoryDir = "./Testdata/models.geo.admin.testdata";
            var reader = new LocalRepositoryReader(repositoryDir);

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
        public async Task TestGetRepositoryFileStreamWithDifferentPathStyles()
        {
            var repositoryDir = "./Testdata/models.geo.admin.testdata";
            var reader = new LocalRepositoryReader(repositoryDir);

            await using var stream0 = await reader.GetRepositoryFileStream("./ARE/SectoralPlans_Catalogues_V1_4.xml");
            await using var stream1 = await reader.GetRepositoryFileStream(@"ARE\SectoralPlans_Catalogues_V1_4.xml");
            await using var stream2 = await reader.GetRepositoryFileStream(@".\ARE\SectoralPlans_Catalogues_V1_4.xml");
            await using var stream3 = await reader.GetRepositoryFileStream(@"ARE/SectoralPlans_Catalogues_V1_4.xml");
        }

        [TestMethod]
        public async Task TestGetRepositoryFileStreamFileNotFound()
        {
            var repositoryDir = "./Testdata/models.geo.admin.testdata";
            var reader = new LocalRepositoryReader(repositoryDir);

            var ex = await Assert.ThrowsExceptionAsync<RepositoryReaderException>(async () =>
            {
                await using var stream = await reader.GetRepositoryFileStream("NonExistentFile.xml");
            });
            Assert.AreEqual("Error reading file <NonExistentFile.xml> from local repository <./Testdata/models.geo.admin.testdata>", ex.Message);
            Assert.IsInstanceOfType(ex.InnerException, typeof(FileNotFoundException));

            var ex1 = await Assert.ThrowsExceptionAsync<RepositoryReaderException>(async () =>
            {
                await using var stream = await reader.GetRepositoryFileStream("");
            });
            Assert.AreEqual("Error reading file <> from local repository <./Testdata/models.geo.admin.testdata>", ex1.Message);
            Assert.IsInstanceOfType(ex.InnerException, typeof(FileNotFoundException));
        }
    }
}
