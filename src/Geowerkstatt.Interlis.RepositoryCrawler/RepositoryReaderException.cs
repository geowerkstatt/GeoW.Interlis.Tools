namespace Geowerkstatt.Interlis.RepositoryCrawler
{
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
