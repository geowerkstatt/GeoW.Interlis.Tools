namespace Geowerkstatt.Interlis.RepositoryCrawler;

public class RepositoryCrawlerOptions
{
    public const string SectionName = "RepositoryCrawler";

    /// <summary>
    /// The root repository that is the starting point for the crawler.
    /// </summary>
    public required string RootRepositoryUri { get; set; }

    /// <summary>
    /// A list of Repository URLs that are ignored while crawling the repository tree.
    /// </summary>
    public IList<string> RepositoryIgnoreList { get; set; } = new List<string>();

    /// <summary>
    /// If the repository data is older than this timespan, the repository tree is crawled again before searching.
    /// </summary>
    public TimeSpan StaleTime { get; set; } = new TimeSpan(1, 0, 0);

    /// <summary>
    /// The folder where the repository crawler cache database is stored.
    /// </summary>
    public string CacheDbFolder { get; set; } = Path.Combine(Path.GetTempPath(), "Geowerkstatt.Interlis");
}
