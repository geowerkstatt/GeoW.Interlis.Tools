using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;
using System.Net.Http.Headers;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

internal static class CrawlerHelperExtensions
{
    /// <summary>
    /// The User-Agent the crawler identifies itself with. <see cref="HttpClient"/> sends none by default, and some
    /// repositories reject requests without one: models.kgk-cgc.ch answers 403 to cloud hosts such as GitHub-hosted
    /// runners, which then miss all cantonal repositories it lists.
    /// </summary>
    private static readonly ProductInfoHeaderValue UserAgent = new(
        "Geowerkstatt.Interlis.RepositoryCrawler",
        typeof(CrawlerHelperExtensions).Assembly.GetName().Version?.ToString(3));

    /// <summary>
    /// Sends a request with the crawler's User-Agent, unless the caller configured its own on the
    /// <paramref name="httpClient"/>.
    /// </summary>
    public static async Task<HttpResponseMessage> SendWithUserAgentAsync(this HttpClient httpClient, HttpMethod method, Uri uri)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            request.Headers.UserAgent.Add(UserAgent);
        }

        return await httpClient.SendAsync(request).ConfigureAwait(false);
    }

    public static Uri Append(this Uri baseUri, string relativePath)
    {
        string a = baseUri.AbsoluteUri.TrimEnd('/');
        string b = relativePath.TrimStart('/');
        return new Uri($"{a}/{b}");
    }

    public static IEnumerable<Catalog> RemovePrecursorCatalogVersions(this IEnumerable<Catalog> catalogs)
        => catalogs
        .GroupBy(c => c.Identifier)
        .SelectMany(group =>
        {
            var precurserVersions = group.Select(c => c.PrecursorVersion);
            return group.Where(c => !precurserVersions.Contains(c.Version));
        });
}
