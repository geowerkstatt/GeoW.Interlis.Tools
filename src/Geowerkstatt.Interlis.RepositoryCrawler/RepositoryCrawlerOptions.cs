namespace Geowerkstatt.Interlis.RepositoryCrawler;

public class RepositoryCrawlerOptions
{
    public const string SectionName = "Crawler";

    /// <summary>
    /// The root repository that is the starting point for the crawler.
    /// </summary>
    public required string RootRepositoryUri { get; set; }

    /// <summary>
    /// A list of Repository URLs that are ignored while crawling the repository tree.
    /// </summary>
    public IList<string> RepositoryIgnoreList { get; set; } = new List<string>();
}
