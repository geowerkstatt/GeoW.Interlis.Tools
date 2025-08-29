using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

public static class RepositoryReaderExtensions
{
    private const string ModelCode = "http://codes.interlis.ch/model/";
    private const string CatalogCode = "http://codes.interlis.ch/type/referenceData";
    private const string MetaconfigCode = "http://codes.interlis.ch/type/metaconfig";

    public static bool IsCatalog(this DatasetMetadata dataset)
        => dataset.HasCategory(CatalogCode);

    public static bool IsMetaconfig(this DatasetMetadata dataset)
        => dataset.HasCategory(MetaconfigCode);

    public static bool HasCategory(this DatasetMetadata dataset, string categoryCode)
        => dataset.categories?.Any(c => categoryCode.Equals(c.value, StringComparison.Ordinal)) ?? false;

    public static List<string> GetReferencedModels(this DatasetMetadata data)
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

    public static string GetDefaultTitle(this DatasetMetadata data)
        => data.title?.MultilingualText?.LocalisedTexts?.FirstOrDefault(lt => string.Empty.Equals(lt.Language, StringComparison.OrdinalIgnoreCase))?.Text ?? string.Empty;

    public static List<string> GetFiles(this DatasetMetadata data)
        => data.files?
            .Where(f => f.file is not null)
            .SelectMany(f => f.file)
            .Select(f => f.path)
            .ToList()
        ?? new List<string>();
}
