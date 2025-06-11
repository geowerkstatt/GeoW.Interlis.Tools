using System.ComponentModel.DataAnnotations;

namespace Geowerkstatt.Interlis.RepositoryCrawler.Models;

public class InterlisFile
{
    [Key]
    public required string MD5 { get; set; }

    public required string Content { get; set; }

    public ICollection<Model> Models { get; set; } = new List<Model>();
}
