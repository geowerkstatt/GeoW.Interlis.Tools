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
        /// </summary>
        /// <param name="repositoryLocation"></param>
        /// <param name="httpClient"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the provided repository location is not supported.</exception>
        public static RepositoryReader Create(string repositoryLocation, HttpClient? httpClient = null)
        {
            if (Uri.TryCreate(repositoryLocation, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    return new HttpRepositoryReader(uri, httpClient);

                if(uri.Scheme == Uri.UriSchemeFile)
                    return new LocalRepositoryReader(new DirectoryInfo(uri.LocalPath));

                // Throw exception for unsupported URI schemes
                throw new RepositoryReaderException($"The repository location <{repositoryLocation}> is not supported. Only local file paths and HTTP(s) URLs are supported.");
            }

            // Throw exception if the location does not represent any valid type of location
            throw new RepositoryReaderException($"The repository location <{repositoryLocation}> is not a valid location.");
        }
    }
}
