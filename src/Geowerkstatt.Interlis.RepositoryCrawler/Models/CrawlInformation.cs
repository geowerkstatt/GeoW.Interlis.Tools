using System.ComponentModel.DataAnnotations;

namespace Geowerkstatt.Interlis.RepositoryCrawler.Models;

public class CrawlInformation
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The time when the crawl was performed.
    /// </summary>
    public DateTime CrawlTime { get; set; }
}
