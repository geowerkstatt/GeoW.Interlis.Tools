namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    /// <summary>
    /// Provides functionality to read INTERLIS repository data from a repository over HTTP(s).
    /// </summary>
    public class HttpRepositoryReader : RepositoryReader
    {
        private readonly Uri repositoryUri;
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRepositoryReader"/> for the repository at the specified HTTP(s) URL.
        /// </summary>
        /// <param name="repositoryUri"></param>
        /// <param name="httpClient"></param>
        public HttpRepositoryReader(Uri repositoryUri, HttpClient? httpClient = null)
        {
            this.repositoryUri = repositoryUri;
            this.httpClient = httpClient ?? new HttpClient();
        }

        /// <inheritdoc />
        public async override Task<Stream> GetRepositoryFileStream(string filePath)
        {
            try
            {
                var fullFilePath = new Uri(repositoryUri, filePath);

                var response = await httpClient.GetAsync(fullFilePath).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = response.Content;

                return await content.ReadAsStreamAsync().ConfigureAwait(false);
            } catch (Exception ex)
            {
                throw new RepositoryReaderException($"Error reading file <{filePath}> from HTTP repository <{repositoryUri}>", ex);
            }
        }
    }
}
