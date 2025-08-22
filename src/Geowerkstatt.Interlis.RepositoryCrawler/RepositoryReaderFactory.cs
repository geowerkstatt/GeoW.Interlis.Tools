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
        public static RepositoryReader Create(string repositoryLocation, HttpClient? httpClient = null)
        {
            if (Uri.TryCreate(repositoryLocation, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    return new HttpRepositoryReader(uri, httpClient);
            }

            return new LocalRepositoryReader(repositoryLocation);
        }
    }
}
