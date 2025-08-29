using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    /// <summary>
    /// Abstract base class for reading INTERLIS repository data.
    /// </summary>
    public abstract class RepositoryReader
    {
        protected const string IliDataFileName = "ilidata.xml";
        protected const string IliSiteFileName = "ilisite.xml";
        protected const string IliModelsFileName = "ilimodels.xml";

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
        public virtual async Task<IEnumerable<DatasetMetadata>> ReadIliData()
        {
            try
            {
                await using var stream = await GetRepositoryFileStream(IliDataFileName).ConfigureAwait(false);
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
        public virtual async Task<Site?> ReadIliSite()
        {
            try
            {
                await using var stream = await GetRepositoryFileStream(IliSiteFileName).ConfigureAwait(false);
                return RepositoryFilesDeserializer.ParseIliSite(stream);
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                throw new RepositoryReaderException("Error parsing ilisite.xml content.", ex);
            }
        }

        /// <summary>
        /// Reads and parses the ilimodels.xml file from the repository.
        /// </summary>
        /// <returns>The full content of the ilimodels.xml parsed to a list of <see cref="ModelMetadata"/>.</returns>
        /// <exception cref="RepositoryReaderException">If the data from the stream could not be parsed.</exception>
        public virtual async Task<IEnumerable<ModelMetadata>> ReadIliModels()
        {
            try
            {
                await using var stream = await GetRepositoryFileStream(IliModelsFileName).ConfigureAwait(false);
                return RepositoryFilesDeserializer.ParseIliModels(stream);
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                throw new RepositoryReaderException("Error parsing ilimodels.xml content.", ex);
            }
        }
    }
}
