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
        public override Task<Stream> GetRepositoryFileStream(string filePath)
        {
            try
            {
                var fullFilePath = Path.Combine(NormalizePath(repositoryDir), NormalizePath(filePath));
                if (!File.Exists(fullFilePath))
                {
                    throw new FileNotFoundException($"File not found <{Path.GetFullPath(fullFilePath)}>");
                }

                Stream stream = File.OpenRead(fullFilePath);
                return Task.FromResult(stream);
            } 
            catch (Exception ex)
            {
                throw new RepositoryReaderException($"Error reading file <{filePath}> from local repository <{repositoryDir}>", ex);
            }
        }

        private string NormalizePath(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
