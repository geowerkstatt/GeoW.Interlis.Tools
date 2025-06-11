namespace Geowerkstatt.Interlis.RepositoryCrawler.Models;

public class Catalog
{
    public int CatalogId { get; set; }

    public required string Identifier { get; set; }

    public required string Version { get; set; }

    public string? PrecursorVersion { get; set; }

    public DateTime? PublishingDate { get; set; }

    public string? Owner { get; set; }

    public required List<string> File { get; set; }

    public string? Title { get; set; }

    public List<string> ReferencedModels { get; set; } = new List<string>();

    public Repository? ModelRepository { get; set; }
}
