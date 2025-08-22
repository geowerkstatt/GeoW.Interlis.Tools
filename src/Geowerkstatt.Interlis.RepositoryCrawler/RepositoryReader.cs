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

        /// <summary>
        /// Reads and parses the ilidata.xml file from the repository.
        /// </summary>
        /// <returns>Returns the full content of the ilidata.xml as a list of <see cref="DatasetMetadata"/>.</returns>
        public abstract Task<IEnumerable<DatasetMetadata>> ReadIliData();

        /// <summary>
        /// Parses INTERLIS dataset metadata from the provided XML stream.
        /// </summary>
        /// <param name="xmlStream">A stream containing the ilidata.xml content.</param>
        /// <returns>Returns the full content of the ilidata.xml as a list of <see cref="DatasetMetadata"/>.</returns>
        /// <exception cref="RepositoryReaderException">If the data from the stream could not be parsed.</exception>
        public IEnumerable<DatasetMetadata> ReadIliData(Stream xmlStream)
        {
            try
            {
                return RepositoryFilesDeserializer.ParseIliData(xmlStream);
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                throw new RepositoryReaderException("Error parsing ilidata.xml content.", ex);
            }
        }
    }
}
