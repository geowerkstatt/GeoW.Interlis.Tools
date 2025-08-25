using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    /// <summary>
    /// Abstract base class for reading INTERLIS repository data.
    /// </summary>
    public abstract class RepositoryReader
    {
        /// <summary>
        /// The default file name for INTERLIS dataset metadata.
        /// </summary>
        protected const string IliDataFileName = "ilidata.xml";
        protected const string IliSiteFileName = "ilisite.xml";

        /// <summary>
        /// Opens and returns a stream to read a specific file from the repository.
        /// </summary>
        /// <param name="filePath">Path to the file relative to the repository root.</param>
        /// <returns>A stream to the specific file from the repository.</returns>
        /// <exception cref="RepositoryReaderException">If a stream to the specified file in the repository could not be opened.</exception>
        public abstract Task<Stream> GetRepositoryFileStream(string filePath);

        /// <summary>
        /// Reads and parses the ilidata.xml file from the repository.
        /// </summary>
        /// <returns>The full content of the ilidata.xml parsed to a list of <see cref="DatasetMetadata"/>.</returns>
        /// <exception cref="RepositoryReaderException">If the data from the stream could not be parsed.</exception>
        public async Task<IEnumerable<DatasetMetadata>> ReadIliData()
        {
            try
            {
                var stream = await GetRepositoryFileStream(IliDataFileName).ConfigureAwait(false);
                return RepositoryFilesDeserializer.ParseIliData(stream);
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                throw new RepositoryReaderException("Error parsing ilidata.xml content.", ex);
            }
        }

        /// <summary>
        /// Reads and parses the ilisite.xml file from the repository.
        /// </summary>
        /// <returns>The full content of the ilisite.xml parsed to a <see cref="Site"/>.</returns>
        /// <exception cref="RepositoryReaderException">If the data from the stream could not be parsed.</exception>
        public async Task<Site?> ReadIliSite()
        {
            try
            {
                var stream = await GetRepositoryFileStream(IliSiteFileName).ConfigureAwait(false);
                return RepositoryFilesDeserializer.ParseIliSite(stream);
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                throw new RepositoryReaderException("Error parsing ilisite.xml content.", ex);
            }
        }
    }
}
