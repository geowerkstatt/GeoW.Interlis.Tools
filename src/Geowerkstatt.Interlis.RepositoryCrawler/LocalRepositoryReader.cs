using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    /// <summary>
    /// Provides functionality to read INTERLIS repository data from a local directory.
    /// </summary>
    public class LocalRepositoryReader : RepositoryReader
    {
        private readonly string repositoryDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalRepositoryReader"/> for the specified local repository.
        /// </summary>
        /// <param name="repositoryDir">Path to the local repository</param>
        public LocalRepositoryReader(string repositoryDir)
        {
            this.repositoryDir = repositoryDir;
        }

        /// <inheritdoc />
        public override Task<IEnumerable<DatasetMetadata>> ReadIliData()
        {
            try
            {
                var ilidataFilePath = Path.Combine(repositoryDir, IliDataFileName);
                if (!File.Exists(ilidataFilePath))
                    throw new FileNotFoundException($"File not found: {ilidataFilePath}");

                var result = ReadIliData(File.OpenRead(ilidataFilePath));

                return Task.FromResult(result);
            }
            catch (Exception ex) when (ex is not RepositoryReaderException)
            {
                throw new RepositoryReaderException($"Error reading from local repository: {repositoryDir}", ex);
            }
        }
    }
}
