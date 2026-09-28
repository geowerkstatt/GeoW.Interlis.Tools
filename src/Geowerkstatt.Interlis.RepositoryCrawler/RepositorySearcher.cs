using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

/// <summary>
/// Provides methods to search INTERLIS repositories.
/// The crawling of the repository tree is delegated to <see cref="IRepositoryCrawler"/> while the
/// <see cref="RepositorySearcher"/> manages a cache of the crawled repository data and provides the search methods.
/// The searcher is safe for concurrent use: searches running at the same time share one crawl of the repository
/// tree and one creation of the cache database (see <see cref="databaseLock"/>). Several processes may share the
/// cache database too (every VS Code window runs its own language server): the database is created by whichever
/// process comes first, a tree another process refreshed meanwhile is kept instead of being replaced, and a file
/// another search cached meanwhile is not stored twice (see <see cref="SaveFileCacheAsync"/>).
/// </summary>
public class RepositorySearcher
{
    /// <summary>SQLite's extended result codes for a violated primary key and a violated unique index.</summary>
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;

    /// <summary>
    /// One lock per cache database, shared by every searcher of the process that uses that database, since a
    /// process may create several searchers for the same cache. Held while the database is created and while the
    /// repository tree is checked for staleness and refreshed, so that concurrent searches, which would all find the
    /// tree stale at once, crawl it once: the waiters check again after the first one refreshed it. Keyed by the
    /// database's full path, compared as the file system compares paths, so that two configurations naming the
    /// same file in different case on Windows share the lock as they share the file.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> DatabaseLocks = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    private readonly ILogger logger;
    private readonly IRepositoryCrawler repositoryCrawler;
    private readonly RepositoryCrawlerOptions options;
    private readonly DbContextOptions<RepositoryCrawlerContext> contextOptions;
    private readonly SemaphoreSlim databaseLock;

    /// <summary>Whether this searcher has made sure the database exists; read and written under <see cref="databaseLock"/>.</summary>
    private bool databaseCreated;

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
        var dbPath = Path.GetFullPath(Path.Combine(options.CacheDbFolder, dbFileName));

        contextOptions = new DbContextOptionsBuilder<RepositoryCrawlerContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        databaseLock = DatabaseLocks.GetOrAdd(dbPath, _ => new SemaphoreSlim(1, 1));
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
    /// <param name="cancellationToken">Cancels the search; see <see cref="SearchModels"/>.</param>
    /// <returns>A <see cref="Model"/> that has the specified <paramref name="modelName"/>.</returns>
    public async Task<Model?> SearchModel(string modelName, CancellationToken cancellationToken = default)
    {
        var models = await SearchModels(m => EF.Functions.Collate(m.Name, "BINARY") == modelName, cancellationToken).ConfigureAwait(false);

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
    /// <param name="cancellationToken">
    /// Cancels the search: waiting for a refresh another search is running, and the search between two files.
    /// A crawl or a fetch under way runs on, since the crawler cannot be cancelled; a crawl completes for the
    /// searches waiting for it, and the files fetched so far are still cached for the next search.
    /// </param>
    /// <returns>A collection of <see cref="Model"/>.</returns>
    public async Task<IList<Model>> SearchModels(Expression<Func<Model, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using var context = new RepositoryCrawlerContext(contextOptions);
        await RefreshRepositoryTreeIfStaleAsync(context, cancellationToken).ConfigureAwait(false);

        var models = await context.Models
            .Include(m => m.ModelRepository)
            .Where(predicate)
            .OrderByDescending(m => m.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var model in models)
        {
            // Stop fetching, but save what was fetched before giving up.
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

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

        await SaveFileCacheAsync(context).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return models;
    }

    /// <summary>
    /// Saves the files fetched by a search and the references pointing at them. Another search, of this process or
    /// another, may have cached the same file meanwhile, since the fetches run without a lock: the save then fails
    /// on the unique key, and the entries that exist by now are dropped (or turned into updates when they point at
    /// another hash) before saving again. A reference that another process pruned meanwhile fails its update
    /// instead and is inserted again, while a model row a refresh replaced meanwhile is left to the refreshed tree.
    /// A cache that still cannot be saved is logged and left as it is: the search has its files, and the next
    /// search fetches them again. The save is not cancelled with the search: the files are downloaded by then, and
    /// caching them is what spares the next search the download.
    /// </summary>
    private async Task SaveFileCacheAsync(RepositoryCrawlerContext context)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await context.SaveChangesAsync().ConfigureAwait(false);
                return;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < 3)
            {
                // A row that is gone meanwhile: a reference pruned by another process is inserted again. Any other
                // row (a model whose hash the crawler adopted, see RepositoryCrawler.FetchInterlisFile) went with a
                // refresh of the tree and is dropped, so that the refreshed tree is not populated with stale rows.
                foreach (var entry in ex.Entries)
                {
                    entry.State = entry.Entity is InterlisFileReference ? EntityState.Added : EntityState.Detached;
                }
            }
            catch (DbUpdateException ex) when (attempt < 3 && ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintPrimaryKey or SqliteConstraintUnique })
            {
                // A row with the same key was inserted meanwhile. Only these two constraints mean that; a violated
                // NOT NULL or foreign key is a bug, which the catch below logs.
                DropEntriesCachedMeanwhile(context);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Unable to update the file cache; the fetched files are not cached.");
                context.ChangeTracker.Clear();
                return;
            }
        }
    }

    /// <summary>
    /// Turns the file and reference inserts of this search that another search has done meanwhile into no-ops, or
    /// into updates where the reference stored meanwhile points at another hash than the one fetched here.
    /// </summary>
    private static void DropEntriesCachedMeanwhile(RepositoryCrawlerContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<InterlisFile>().Where(entry => entry.State == EntityState.Added).ToList())
        {
            if (context.InterlisFiles.AsNoTracking().Any(file => file.MD5 == entry.Entity.MD5))
            {
                entry.State = EntityState.Detached;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<InterlisFileReference>().Where(entry => entry.State == EntityState.Added).ToList())
        {
            var url = entry.Entity.SourceUrl;
            if (context.InterlisFileReferences.AsNoTracking().FirstOrDefault(reference => reference.SourceUrl == url) is { } existing)
            {
                entry.State = existing.MD5.Equals(entry.Entity.MD5, StringComparison.OrdinalIgnoreCase) ? EntityState.Detached : EntityState.Modified;
            }
        }
    }

    /// <summary>
    /// Makes sure the cache database exists and holds a repository tree that is not older than the configured stale
    /// time, crawling the tree when it is missing or stale. Serialized through <see cref="databaseLock"/>: a search
    /// that finds the tree stale while another one is already refreshing it waits for that refresh and then finds
    /// the tree fresh instead of crawling it again. The token only cancels waiting for the lock: what happens under
    /// the lock serves every waiter, so it is not cancelled on behalf of one of them.
    /// </summary>
    private async Task RefreshRepositoryTreeIfStaleAsync(RepositoryCrawlerContext context, CancellationToken cancellationToken)
    {
        await databaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!databaseCreated)
            {
                await EnsureDatabaseCreatedAsync(context).ConfigureAwait(false);
                databaseCreated = true;
            }

            var lastCrawl = await LastCrawlTimeAsync(context).ConfigureAwait(false);
            if (await IsRepositoryTreeStaleAsync(context, lastCrawl).ConfigureAwait(false))
            {
                await UpdateRepositoryTreeAsync(context, lastCrawl).ConfigureAwait(false);
            }
        }
        finally
        {
            databaseLock.Release();
        }
    }

    /// <summary>
    /// Creates the database with its tables unless it exists. Another process may create it between the check and
    /// the creation, in which case the creation fails on a table that already exists; the database is then complete
    /// (a creation is one transaction), so that failure means the same as finding it created.
    /// </summary>
    private async Task EnsureDatabaseCreatedAsync(RepositoryCrawlerContext context)
    {
        try
        {
            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        }
        catch (SqliteException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(ex, "The cache database was created by another process meanwhile.");
        }
    }

    /// <summary>When the cached repository tree was crawled, or <see langword="null"/> when it never was.</summary>
    private static Task<DateTime?> LastCrawlTimeAsync(RepositoryCrawlerContext context)
        => context.CrawlInformations.MaxAsync(ci => (DateTime?)ci.CrawlTime);

    /// <summary>Whether the cached repository tree is missing or was crawled (at <paramref name="lastCrawlTime"/>) longer ago than the configured stale time.</summary>
    private async Task<bool> IsRepositoryTreeStaleAsync(RepositoryCrawlerContext context, DateTime? lastCrawlTime)
        => lastCrawlTime == null || DateTime.Now > lastCrawlTime + options.StaleTime || !await context.Repositories.AnyAsync().ConfigureAwait(false);

    /// <summary>
    /// Trims the file cache to the freshly crawled <paramref name="repositories"/> tree, in two steps:
    /// first drops every <see cref="InterlisFileReference"/> whose URL is no longer served by any model, then drops
    /// every <see cref="InterlisFile"/> that is left unreferenced. This reclaims content superseded by a changed file
    /// (its reference was repointed to the new hash) as well as files of models that disappeared from the tree,
    /// while never removing content that is still reachable from some URL.
    /// </summary>
    private static async Task PruneFileCacheAsync(RepositoryCrawlerContext context, IDictionary<string, Repository> repositories)
    {
        var activeUrls = repositories.Values
            .SelectMany(repository => repository.Models)
            .Select(model => model.Uri?.AbsoluteUri)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        // Load the references and filter in memory: the active-URL set can be large, so an "IN (...)" translation
        // could exceed the SQLite parameter limit. The reference table is bounded by the number of cached files.
        var staleReferences = (await context.InterlisFileReferences.ToListAsync().ConfigureAwait(false))
            .Where(reference => !activeUrls.Contains(reference.SourceUrl))
            .ToList();
        context.InterlisFileReferences.RemoveRange(staleReferences);
        await context.SaveChangesAsync().ConfigureAwait(false);

        await context.InterlisFiles
            .Where(file => !context.InterlisFileReferences.Any(reference => reference.MD5 == file.MD5))
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Crawls the repository tree and replaces the cached one with it, unless another process has refreshed the
    /// cache since it was found stale: the crawl runs outside the database transaction (it takes a while, and the
    /// transaction takes SQLite's write lock as soon as it begins), so the crawl time in the database is looked at
    /// again once the lock is held, and a tree crawled at another time than the one seen
    /// (<paramref name="lastCrawlSeen"/>) is kept. The times are compared for equality rather than against the
    /// clock: the other process assigns its crawl time before its commit, so it may lie before this search's check.
    /// </summary>
    private async Task UpdateRepositoryTreeAsync(RepositoryCrawlerContext context, DateTime? lastCrawlSeen)
    {
        var repositories = await repositoryCrawler.CrawlModelRepositories(options).ConfigureAwait(false);

        try
        {
            // BEGIN IMMEDIATE: waits for the write transaction of another process and fails once the busy timeout
            // has passed, which is a failed refresh like any other, not a failed search.
            using var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false);

            if (await LastCrawlTimeAsync(context).ConfigureAwait(false) != lastCrawlSeen)
            {
                logger.LogInformation("The repository tree was refreshed by another process meanwhile; keeping it.");
                return;
            }

            await context.Catalogs.ExecuteDeleteAsync().ConfigureAwait(false);
            await context.Models.ExecuteDeleteAsync().ConfigureAwait(false);
            await context.Repositories.ExecuteDeleteAsync().ConfigureAwait(false);
            await context.CrawlInformations.ExecuteDeleteAsync().ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            context.Repositories.AddRange(repositories.Values);
            context.CrawlInformations.Add(new CrawlInformation
            {
                CrawlTime = DateTime.Now,
            });
            await context.SaveChangesAsync().ConfigureAwait(false);

            // Now that the current tree is persisted, trim the file cache to the URLs it still serves.
            await PruneFileCacheAsync(context, repositories).ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);
            logger.LogInformation("Updating ModelRepoDatabase complete. Inserted {RepositoryCount} repositories.", repositories.Count);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            // A DbUpdateException (EF Core's, not a DbException) is what a failed SaveChanges throws. The rolled
            // back tree is dropped from the context, so that the search does not try to insert it with its files.
            logger.LogError(ex, "Unable to update ModelRepoDatabase");
            context.ChangeTracker.Clear();
        }
    }

    internal async Task DeleteCacheDatabaseAsync()
    {
        using var context = new RepositoryCrawlerContext(contextOptions);
        await context.Database.EnsureDeletedAsync();
    }
}
