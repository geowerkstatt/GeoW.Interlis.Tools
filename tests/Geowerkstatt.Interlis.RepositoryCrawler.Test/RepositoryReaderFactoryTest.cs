using System.Reflection.PortableExecutable;

namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    [TestClass]
    public class RepositoryReaderFactoryTest
    {
        [TestMethod]
        public void CreateHttpRepositoryReaders()
        {
            var reader1 = RepositoryReaderFactory.Create("http://example.com/repository/path");
            Assert.IsInstanceOfType(reader1, typeof(HttpRepositoryReader));

            var reader2 = RepositoryReaderFactory.Create("https://example.com/repository/path");
            Assert.IsInstanceOfType(reader2, typeof(HttpRepositoryReader));
        }

        [TestMethod]
        public void CreateWithUnsupportedLocation()
        {
            Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create("scheme://example.com/repository/path")); // Valid URI but unsupported scheme
            Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create("ftp://example.com/repository/path")); // FTP not supported (yet)
        }

        [TestMethod]
        public void CreateWithEmptyString()
        {
            var ex = Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create(string.Empty));
            Assert.AreEqual("The repository location must not be empty.", ex.Message);
        }

        [TestMethod]
        public void CreateWithWhitespaceString()
        {
            var ex = Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create("  "));
            Assert.AreEqual("The repository location must not be empty.", ex.Message);
        }

        [TestMethod]
        public void CreateLocalRepositoryReader()
        {
            var existingPath = "./";
            Assert.IsTrue(Directory.Exists(existingPath));
            var reader = RepositoryReaderFactory.Create(existingPath);
            Assert.IsInstanceOfType(reader, typeof(LocalRepositoryReader));
        }

        [TestMethod]
        public void CreateWithNonExistingPath()
        {
            var nonExistingPath = $"./nonexistent_{Guid.NewGuid()}";
            Assert.IsFalse(Directory.Exists(nonExistingPath));
            var ex = Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create(nonExistingPath));
            Assert.AreEqual("The repository location <" + nonExistingPath + "> is not a valid location", ex.Message);
        }

        [TestMethod]
        public void CreateWithInvalidPath()
        {
            var invalidPath = $"<invalid path>";
            Assert.IsFalse(Directory.Exists(invalidPath));
            var ex = Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create(invalidPath));
            Assert.AreEqual("The repository location <" + invalidPath + "> is not a valid location", ex.Message);
        }
    }
}
