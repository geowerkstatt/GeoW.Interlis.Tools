using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

internal static class CrawlerHelperExtensions
{
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
