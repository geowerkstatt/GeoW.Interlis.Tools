using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Geowerkstatt.Interlis.RepositoryCrawler.Models;

public class Repository
{
    [Key]
    public required string HostNameId { get; set; }

    public required Uri Uri { get; set; }

    public required string Name { get; set; }

    public string? Title { get; set; }

    public string? ShortDescription { get; set; }

    public string? Owner { get; set; }

    public string? TechnicalContact { get; set; }

    public ISet<Repository> SubsidiarySites { get; set; } = new HashSet<Repository>();

    [JsonIgnore]
    public ISet<Repository> ParentSites { get; set; } = new HashSet<Repository>();

    public ISet<Model> Models { get; set; } = new HashSet<Model>();

    public ISet<Catalog> Catalogs { get; set; } = new HashSet<Catalog>();
}
