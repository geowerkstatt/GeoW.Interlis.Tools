using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

/// <summary>
/// Provides methods to search INTERLIS repositories.
/// The crawling of the repository tree is delegated to <see cref="IRepositoryCrawler"/> while the
/// <see cref="RepositorySearcher"/> manages a cache of the crawled repository data and provides the search methods.
/// </summary>
public class RepositorySearcher
{
    private readonly ILogger logger;
    private readonly IRepositoryCrawler repositoryCrawler;
    private readonly RepositoryCrawlerOptions options;
    private readonly DbContextOptions<RepositoryCrawlerContext> contextOptions;

    /// <summary>
    /// Create a new <see cref="RepositorySearcher"/>.
    /// </summary>
    /// <param name="repositoryCrawler">The <see cref="IRepositoryCrawler"/> to use for fetching repository data.</param>
    /// <param name="configuration">The configuration containing the <see cref="RepositoryCrawlerOptions"/>.</param>
    /// <param name="loggerFactory">A factory to create <see cref="ILogger"/> instances.</param>
    /// <exception cref="InvalidOperationException">Invalid <paramref name="configuration"/>.</exception>
    public RepositorySearcher(IRepositoryCrawler repositoryCrawler, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<RepositorySearcher>();
        this.repositoryCrawler = repositoryCrawler;

        var options = configuration.GetSection(RepositoryCrawlerOptions.SectionName).Get<RepositoryCrawlerOptions>();
        if (options == null)
        {
            logger.LogError($"Unable to parse configuration {RepositoryCrawlerOptions.SectionName}.");
            throw new InvalidOperationException($"Configuration section '{RepositoryCrawlerOptions.SectionName}' is missing or invalid.");
        }

        this.options = options;
        Directory.CreateDirectory(options.CacheDbFolder);
        var dbFileName = $"ModelRepositoryCache{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? ""}.db";

        contextOptions = new DbContextOptionsBuilder<RepositoryCrawlerContext>()
            .UseSqlite($"Data Source={Path.Combine(options.CacheDbFolder, dbFileName)}")
            .Options;
    }

    /// <summary>
    /// Create a new <see cref="RepositorySearcher"/> with default <see cref="IRepositoryCrawler"/> and configuration from "appsettings.json".
    /// </summary>
    /// <param name="loggerFactory">A factory to create <see cref="ILogger"/> instances.</param>
    public RepositorySearcher(ILoggerFactory loggerFactory) : this(
        new RepositoryCrawler(loggerFactory, new HttpClient()),
        new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build(),
        loggerFactory)
    {
    }

    /// <summary>
    /// Search the model with <paramref name="modelName"/> in repositories. The <see cref="Model.FileContent"/> of the found <see cref="Model"/>s is either populated from the
    /// cache or fetched from the repository.
    /// </summary>
    /// <param name="modelName">The model name to search the definition file(s) for.</param>
    /// <returns>A <see cref="Model"/> that has the specified <paramref name="modelName"/>.</returns>
    public async Task<Model?> SearchModel(string modelName)
    {
        var models = await SearchModels(m => EF.Functions.Collate(m.Name, "BINARY") == modelName);

        switch (models.Count)
        {
            case 0:
                logger.LogWarning("Model '{ModelName}' not found in {Repository}", modelName, options.RootRepositoryUri);
                return null;
            case 1:
                logger.LogInformation("Found model '{ModelName}' in {Repository}", modelName, models.Single().ModelRepository?.Uri);
                return models.Single();
            default:
                logger.LogWarning("Found model '{ModelName}' in {Repository}. Other models with same name {AlternativeModels}",
                    models.First(),
                    models.First().ModelRepository?.Uri,
                    string.Join(",", models.Skip(1).Select(m => $"{m.Name} \"{m.Version}\" ({m.ModelRepository?.Uri})")));
                return models.First();
        }
    }

    /// <summary>
    /// Search the models that satisfy the <paramref name="predicate"/> in repositories. The <see cref="Model.FileContent"/> of the found <see cref="Model"/>s is either populated from the
    /// cache or fetched from the repository.
    /// </summary>
    /// <param name="predicate">The predicate the <see cref="Model"/>s need to satisfy.</param>
    /// <returns>A collection of <see cref="Model"/>.</returns>
    public async Task<IList<Model>> SearchModels(Expression<Func<Model, bool>> predicate)
    {
        using var context = new RepositoryCrawlerContext(contextOptions);
        context.Database.EnsureCreated();

        var lastCrawl = context.CrawlInformations
            .OrderByDescending(ci => ci.CrawlTime)
            .FirstOrDefault();
        var lastCrawlTime = lastCrawl?.CrawlTime ?? DateTime.MinValue;

        if (!context.Repositories.Any() || DateTime.Now > lastCrawlTime + options.StaleTime) {
            await UpdateRepositoryTree(context).ConfigureAwait(false);
        }

        var models = context.Models
            .Include(m => m.ModelRepository)
            .Where(predicate)
            .OrderByDescending(m => m.Version)
            .ToList();

        foreach (var model in models)
        {
            // A model without a resolvable URL cannot have a file fetched for it.
            var url = model.Uri?.AbsoluteUri;
            if (url == null)
            {
                continue;
            }

            // Resolve the URL to the content hash last fetched from it (Find, unlike a LINQ query, also sees rows
            // added earlier in this loop but not yet saved, so models sharing a file are fetched only once). The
            // reference is looked up once here and reused for the upsert below.
            var fileReference = context.InterlisFileReferences.Find(url);
            var file = await repositoryCrawler
                .FetchInterlisFile(model, _ => fileReference == null ? null : context.InterlisFiles.Find(fileReference.MD5))
                .ConfigureAwait(false);

            if (file == null)
            {
                continue;
            }

            // Store the content once, keyed by its hash (shared by every URL that serves byte-identical content) ...
            if (context.InterlisFiles.Find(file.MD5) == null)
            {
                context.InterlisFiles.Add(file);
            }

            // ... and point this URL at that content.
            if (fileReference == null)
            {
                context.InterlisFileReferences.Add(new InterlisFileReference { SourceUrl = url, MD5 = file.MD5 });
            }
            else if (!fileReference.MD5.Equals(file.MD5, StringComparison.OrdinalIgnoreCase))
            {
                fileReference.MD5 = file.MD5;
            }
        }

        context.SaveChanges();

        return models;
    }

    /// <summary>
    /// Trims the file cache to the freshly crawled <paramref name="repositories"/> tree, in two steps:
    /// first drops every <see cref="InterlisFileReference"/> whose URL is no longer served by any model, then drops
    /// every <see cref="InterlisFile"/> that is left unreferenced. This reclaims content superseded by a changed file
    /// (its reference was repointed to the new hash) as well as files of models that disappeared from the tree,
    /// while never removing content that is still reachable from some URL.
    /// </summary>
    private static void PruneFileCache(RepositoryCrawlerContext context, IDictionary<string, Repository> repositories)
    {
        var activeUrls = repositories.Values
            .SelectMany(repository => repository.Models)
            .Select(model => model.Uri?.AbsoluteUri)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        // Load the references and filter in memory: the active-URL set can be large, so an "IN (...)" translation
        // could exceed the SQLite parameter limit. The reference table is bounded by the number of cached files.
        var staleReferences = context.InterlisFileReferences
            .AsEnumerable()
            .Where(reference => !activeUrls.Contains(reference.SourceUrl))
            .ToList();
        context.InterlisFileReferences.RemoveRange(staleReferences);
        context.SaveChanges();

        context.InterlisFiles
            .Where(file => !context.InterlisFileReferences.Any(reference => reference.MD5 == file.MD5))
            .ExecuteDelete();
    }

    private async Task UpdateRepositoryTree(RepositoryCrawlerContext context)
    {
        var repositories = await repositoryCrawler.CrawlModelRepositories(options);

        using var transaction = context.Database.BeginTransaction();
        try
        {
            context.Catalogs.ExecuteDelete();
            context.Models.ExecuteDelete();
            context.Repositories.ExecuteDelete();
            context.CrawlInformations.ExecuteDelete();
            context.SaveChanges();

            context.Repositories.AddRange(repositories.Values);
            context.CrawlInformations.Add(new CrawlInformation
            {
                CrawlTime = DateTime.Now,
            });
            context.SaveChanges();

            // Now that the current tree is persisted, trim the file cache to the URLs it still serves.
            PruneFileCache(context, repositories);

            transaction.Commit();
            logger.LogInformation("Updating ModelRepoDatabase complete. Inserted {RepositoryCount} repositories.", repositories.Count);
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Unable to update ModelRepoDatabase");
        }
    }

    internal async Task DeleteCacheDatabase()
    {
        using var context = new RepositoryCrawlerContext(contextOptions);
        await context.Database.EnsureDeletedAsync();
    }
}
