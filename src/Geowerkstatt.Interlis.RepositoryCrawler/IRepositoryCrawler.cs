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
    /// Fetches the INTERLIS file for a single <paramref name="model"/>. The <see cref="Model.FileContent"/> of the found <see cref="Model"/>s is either populated from the
    /// <paramref name="getCachedFile"/> or fetched from the repository.
    /// If the <paramref name="model"/> is missing the <see cref="Model.MD5"/> property, it is set according to the downloaded file.
    /// </summary>
    /// <param name="getCachedFile">Get an <see cref="InterlisFile"/> previously fetched by its <see cref="InterlisFile.MD5"/> key.</param>
    /// <param name="model">The model to fetch the INTERLIS file for.</param>
    /// <returns>The <see cref="InterlisFile"/> for the model or <see langword="null"/> if it was not available.</returns>
    Task<InterlisFile?> FetchInterlisFile(Model model, Func<string, InterlisFile?> getCachedFile);
}
