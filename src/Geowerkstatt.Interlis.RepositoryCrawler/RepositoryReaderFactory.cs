namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    /// <summary>
    /// Factory for creating RepositoryReader instances based on the repository location.
    /// </summary>
    public class RepositoryReaderFactory
    {
        /// <summary>
        /// Creates a RepositoryReader instance based on the provided repository location.
        /// Supported repository locations are local file paths and HTTP(s) URLs.
        /// Local paths must point to an existing directory.
        /// </summary>
        /// <param name="repositoryLocation"></param>
        /// <param name="httpClient"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the provided repository location is not supported.</exception>
        public static RepositoryReader Create(string repositoryLocation, HttpClient? httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(repositoryLocation))
            {
                throw new RepositoryReaderException("The repository location must not be empty.");
            }

            // Check for valid HTTP/S URI
            if (Uri.TryCreate(repositoryLocation, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return new HttpRepositoryReader(uri, httpClient);
            }

            // Check if it's an existing local directory
            if (Directory.Exists(repositoryLocation))
            {
                return new LocalRepositoryReader(repositoryLocation);
            }

            throw new RepositoryReaderException($"The repository location <{repositoryLocation}> is not a valid location");
        }
    }
}
