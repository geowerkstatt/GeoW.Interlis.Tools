namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    [TestClass]
    public class RepositoryReaderFactoryTest
    {
        private void AssertReaderTypeForPathVariants(string basePath, Type expectedType)
        {
            var reader = RepositoryReaderFactory.Create(basePath);
            var readerTrailing = RepositoryReaderFactory.Create(
                basePath.EndsWith("/") || basePath.EndsWith("\\") ? basePath : basePath + (basePath.Contains("/") ? "/" : "\\")
            );

            Assert.IsInstanceOfType(reader, expectedType);
            Assert.IsInstanceOfType(readerTrailing, expectedType);
        }

        [TestMethod]
        public void CreateLocalRepositoryReaders()
        {
            AssertReaderTypeForPathVariants("C:\\test\\repository\\path", typeof(LocalRepositoryReader)); // Windows absolute path
            AssertReaderTypeForPathVariants(".\\test\\repository\\path", typeof(LocalRepositoryReader)); // Windows relative path
            AssertReaderTypeForPathVariants("\\\\test\\repository\\path", typeof(LocalRepositoryReader)); // Windows UNC path
            AssertReaderTypeForPathVariants("/test/repository/path", typeof(LocalRepositoryReader)); // Unix absolute path
            AssertReaderTypeForPathVariants("test/repository/path", typeof(LocalRepositoryReader)); // Unix relative path
        }

        [TestMethod]
        public void CreateHttpRepositoryReaders()
        {
            AssertReaderTypeForPathVariants("http://example.com/repository/path", typeof(HttpRepositoryReader));
            AssertReaderTypeForPathVariants("https://example.com/repository/path", typeof(HttpRepositoryReader));
        }

        [TestMethod]
        public void CreateWithInvalidLocation()
        {
            Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create(""));
            Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create("invalid path"));
            Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create("://invalid/repository/path"));
            Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create("./invalid/character>"));
        }

        [TestMethod]
        public void CreateWithUnsupportedLocation()
        {
            Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create("scheme://example.com/repository/path")); // Valid URI but unsupported scheme
            Assert.ThrowsException<RepositoryReaderException>(() => RepositoryReaderFactory.Create("ftp://example.com/repository/path")); // FTP not supported (yet)
        }
    }
}
