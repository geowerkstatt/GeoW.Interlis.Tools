using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

internal static class CrawlerHelperExtensions
{
    private const string ModelCode = "http://codes.interlis.ch/model/";
    private const string CatalogCode = "http://codes.interlis.ch/type/referenceData";

    internal static bool IsCatalog(DatasetMetadata dataset)
        => dataset.categories?.Any(c => CatalogCode.Equals(c.value, StringComparison.Ordinal)) ?? false;

    internal static List<string> GetReferencedModels(this DatasetMetadata data)
    {
        var referencedModels = data.categories?
            .Select(c => c.value)
            .Where(v => v is not null && v.StartsWith(ModelCode, StringComparison.Ordinal))
            .Select(v => v.Substring(ModelCode.Length))
            .ToList()
            ?? new List<string>();

        var basketModelLinks = data.baskets?
            .Select(b => b.model.ModelLink.name.Split('.').First())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList()
            ?? new List<string>();

        return referencedModels
            .Concat(basketModelLinks)
            .Distinct()
            .ToList();
    }

    internal static string GetTitle(this DatasetMetadata data)
        => data.title?.MultilingualText?.LocalisedTexts?.FirstOrDefault(lt => string.Empty.Equals(lt.Language, StringComparison.OrdinalIgnoreCase))?.Language ?? string.Empty;

    internal static List<string> GetFiles(this DatasetMetadata data)
        => data.files?
            .Where(f => f.file is not null)
            .SelectMany(f => f.file)
            .Select(f => f.path)
            .ToList()
        ?? new List<string>();

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
