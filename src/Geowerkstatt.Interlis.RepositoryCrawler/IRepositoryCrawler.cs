using Geowerkstatt.Interlis.RepositoryCrawler.Models;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

/// <summary>
/// Defines methods for reading INTERLIS repository tree with required ilisite.xml (following IliSite09 Model),
/// optional ilimodels.xml (following IliRepository09 and IliRepository20 Models) and optional ilidata.xml (following DatasetIdx16 Model).
/// </summary>
public interface IRepositoryCrawler
{
    /// <summary>
    /// Parse the repository tree from the root repository following subsidiary-Sites links only.
    /// </summary>
    /// <param name="options">The <see cref="RepositoryCrawlerOptions"/> that contain the repository at the root of the model repository tree and other configurations.</param>
    /// <returns>Dictionary containing all repositories found in tree. Repository host is used as key. Repositories contain all found information. Root repository contains full tree. </returns>
    Task<IDictionary<string, Repository>> CrawlModelRepositories(RepositoryCrawlerOptions options);

    /// <summary>
    /// Fetches the INTERLIS files from the <paramref name="repositories"/>. Files are identified by their MD5 hash and only downloaded if not already contained in <paramref name="existingFiles"/>.
    /// If a <see cref="Model"/> is missing the <see cref="Model.MD5"/> property, it is set according to the downloaded file.
    /// </summary>
    /// <param name="existingFiles">The <see cref="InterlisFile"/>s previously fetched.</param>
    /// <param name="repositories">The repositories to fetch the files for.</param>
    Task FetchInterlisFiles(IEnumerable<InterlisFile> existingFiles, IEnumerable<Repository> repositories);
}
