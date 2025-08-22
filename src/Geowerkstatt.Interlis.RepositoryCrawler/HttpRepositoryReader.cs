using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

namespace Geowerkstatt.Interlis.RepositoryCrawler
{
    /// <summary>
    /// Provides functionality to read INTERLIS repository data from a repository over HTTP(s).
    /// </summary>
    public class HttpRepositoryReader : RepositoryReader
    {
        private Uri repositoryUri;
        private HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRepositoryReader"/> for the repository at the specified HTTP(s) URL.
        /// </summary>
        /// <param name="repositoryUri"></param>
        /// <param name="httpClient"></param>
        public HttpRepositoryReader(Uri repositoryUri, HttpClient? httpClient)
        {
            this.repositoryUri = repositoryUri;
            this.httpClient = httpClient ?? new HttpClient();
        }

        /// <inheritdoc />
        public async override Task<IEnumerable<DatasetMetadata>> ReadIliData()
        {
            try
            {
                var ilidataUri = new Uri(repositoryUri, IliDataFileName);

                var response = await httpClient.GetAsync(ilidataUri).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = response.Content;
                await using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);

                return ReadIliData(stream);
            }
            catch (Exception ex)
            {
                throw new RepositoryReaderException($"Error reading from HTTP repository: {repositoryUri}", ex);
            }
        }
    }
}
