namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    /// <summary>
    /// Represents errors that occur during reading from an Interlis repository by the <see cref="RepositoryReader"/>.
    /// </summary>
    public class RepositoryReaderException : Exception
    {
        public RepositoryReaderException()
        {
        }

        public RepositoryReaderException(string? message) : base(message)
        {
        }

        public RepositoryReaderException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
