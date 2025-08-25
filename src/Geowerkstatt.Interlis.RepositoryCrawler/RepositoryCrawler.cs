using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

/// <inheritdoc cref="IRepositoryCrawler"/>
public class RepositoryCrawler : IRepositoryCrawler
{
    private readonly ILogger logger;
    private readonly HttpClient httpClient;

    public RepositoryCrawler(ILoggerFactory loggerFactory, HttpClient httpClient)
    {
        logger = loggerFactory.CreateLogger(GetType());
        this.httpClient = httpClient;
    }

    public async Task<InterlisFile?> FetchInterlisFile(Model model, Func<string, InterlisFile?> getCachedFile)
    {
        // Check cache
        InterlisFile? file = string.IsNullOrEmpty(model.MD5) ? null : getCachedFile(model.MD5);
        if (file != null)
        {
            model.FileContent = file;
            return file;
        }

        // Fetch file from repository
        if (model.Uri != null)
        {
            file = await FetchInterlisFile(model.Uri).ConfigureAwait(false);
        }

        if (file != null)
        {
            if (!file.MD5.Equals(model.MD5, StringComparison.OrdinalIgnoreCase))
            {
                // Reuse file from cache instead of trying to add the same file as a new entity with the same primary key
                file = getCachedFile(file.MD5) ?? file;

                if (string.IsNullOrEmpty(model.MD5))
                {
                    model.MD5 = file.MD5;
                }
                else
                {
                    logger.LogWarning("The MD5 Hash of Model <{Model}> ({MD5Model}) does not match that of the file <{URL}> ({MD5File}).", model.Name, model.MD5, model.Uri, file.MD5);
                }
            }

            model.FileContent = file;
        }

        return file;
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, Repository>> CrawlModelRepositories(RepositoryCrawlerOptions options)
    {
        var modelRepositories = new ConcurrentDictionary<string, Repository>();

        if (!Uri.TryCreate(options.RootRepositoryUri, UriKind.Absolute, out var rootRepositoryUri))
        {
            logger.LogError($"Unable to parse configuration {RepositoryCrawlerOptions.SectionName}:{nameof(RepositoryCrawlerOptions.RootRepositoryUri)}.");
            return modelRepositories;
        }

        var ignoreList = new List<Uri>();
        foreach (var ignoredUrlString in options.RepositoryIgnoreList)
        {
            if (Uri.TryCreate(ignoredUrlString, UriKind.Absolute, out var ignoredUrl))
            {
                ignoreList.Add(ignoredUrl);
            }
            else
            {
                logger.LogWarning($"Unable to parse URL <{ignoredUrlString}> from {RepositoryCrawlerOptions.SectionName}:{nameof(RepositoryCrawlerOptions.RepositoryIgnoreList)}.");
            }
        }

        await CrawlRepositories(rootRepositoryUri, ignoreList, null, modelRepositories).ConfigureAwait(false);
        return modelRepositories;
    }

    private async Task<Repository?> CrawlRepositories(Uri rootRepositoryUri, List<Uri> ignoreList, Repository? parent, IDictionary<string, Repository> accumulatedModelRepositories)
    {
        if (IsIgnored(rootRepositoryUri, ignoreList))
        {
            logger.LogDebug($"Repository <{rootRepositoryUri}> is ignored");
            return null;
        }

        rootRepositoryUri = await PreferHttpsIfAvailable(rootRepositoryUri).ConfigureAwait(false);

        if (accumulatedModelRepositories.TryGetValue(rootRepositoryUri.AbsoluteUri, out var existingRepository))
        {
            if (parent != null)
            {
                existingRepository.ParentSites.Add(parent);
            }

            return existingRepository;
        }

        var (root, subsidiarys) = await AnalyseRepository(rootRepositoryUri, parent).ConfigureAwait(false);
        if (root is not null && accumulatedModelRepositories.TryAdd(rootRepositoryUri.AbsoluteUri, root))
        {
            var subsidiarySites = new HashSet<Repository>();
            foreach (var subsidiary in subsidiarys)
            {
                var subsidiaryRepository = await CrawlRepositories(subsidiary, ignoreList, root, accumulatedModelRepositories).ConfigureAwait(false);
                if (subsidiaryRepository != null)
                {
                    subsidiarySites.Add(subsidiaryRepository);
                }
            }

            root.SubsidiarySites = subsidiarySites;
        }

        return root;
    }

    private async Task<(Repository? Repository, IEnumerable<Uri> SubsidiaryRepositories)> AnalyseRepository(Uri repositoryUri, Repository? parent = null)
    {
        try
        {
            var ilisiteTask = ParseIlisite(repositoryUri);
            var modelsTask = CrawlIlimodels(repositoryUri);
            var metadatasTask = CrawlIlidata(repositoryUri);

            var ilisite = await ilisiteTask.ConfigureAwait(false);
            var models = await modelsTask.ConfigureAwait(false);
            var metadatas = await metadatasTask.ConfigureAwait(false);

            if (ilisite is null) return (null, Enumerable.Empty<Uri>());

            var repository = new Repository
            {
                HostNameId = repositoryUri.AbsoluteUri,
                Uri = repositoryUri,
                Name = ilisite.Name ?? repositoryUri.Host,
                Title = ilisite.Title,
                ShortDescription = ilisite.shortDescription,
                Owner = ilisite.Owner,
                TechnicalContact = ilisite.technicalContact,
                Models = models,
                Catalogs = metadatas,
            };

            if (parent != null)
            {
                repository.ParentSites.Add(parent);
            }

            foreach (var model in models)
            {
                model.ModelRepository = repository;
            }

            var subsidiaryRepositories = ilisite.subsidiarySites?
                    .Where(location => location?.value is not null)
                    .Select(location => new Uri(location.value!))
                    .ToList() ?? Enumerable.Empty<Uri>();

            return (repository, subsidiaryRepositories);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is InvalidOperationException || ex is OperationCanceledException || ex is RepositoryReaderException)
        {
            logger.LogError(ex, "Analysis of {Repository} failed.", repositoryUri);
            return (null, Enumerable.Empty<Uri>());
        }
    }

    private async Task<ISet<Catalog>> CrawlIlidata(Uri repositoryUri)
    {
        try
        {
            var repositoryReader = RepositoryReaderFactory.Create(repositoryUri.AbsoluteUri, httpClient);
            var iliData = await repositoryReader.ReadIliData().ConfigureAwait(false);

            return iliData
                .Select(m => new Catalog
                {
                    Identifier = m.id,
                    Version = m.version,
                    PublishingDate = DateTime.SpecifyKind(m.publishingDate.Date, DateTimeKind.Utc),
                    PrecursorVersion = m.precursorVersion,
                    Owner = m.owner,
                    Title = m.GetTitle(),
                    File = m.GetFiles().Select(f => repositoryUri.Append(f).AbsoluteUri).ToList(),
                    ReferencedModels = m.GetReferencedModels(),
                })
                .RemovePrecursorCatalogVersions()
                .ToHashSet();
        }
        catch (Exception ex) when (ex is RepositoryReaderException || ex is OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not analyse ilidata.xml in repository {RepositoryUri}.", repositoryUri);
        }

        return new HashSet<Catalog>();
    }

    private async Task<ISet<Model>> CrawlIlimodels(Uri repositoryUri)
    {
        var ilimodelsUri = GetIlimodelsUrl(repositoryUri);
        using (var ilimodelsStream = await GetStreamFromUrl(ilimodelsUri).ConfigureAwait(false))
        {
            var models = RepositoryFilesDeserializer.ParseIliModels(ilimodelsStream)
                .Select(model => new Model
                {
                    Name = model.Name,
                    SchemaLanguage = model.SchemaLanguage,
                    File = model.File,
                    Version = model.Version,
                    PublishingDate = model.publishingDate?.ToUniversalTime(),
                    DependsOnModel = model.dependsOnModel.Where(s => !string.IsNullOrEmpty(s?.value)).Select(m => m.value!).ToList(),
                    ShortDescription = model.shortDescription,
                    Title = model.Title,
                    Issuer = model.Issuer,
                    TechnicalContact = model.technicalContact,
                    FurtherInformation = model.furtherInformation,
                    MD5 = model.md5,
                    Tags = model.Tags?.Split(',').Distinct().ToList() ?? new List<string>(),
                })
                .ToHashSet();

            return models;
        }
    }

    private async Task<Site?> ParseIlisite(Uri repositoryUri)
    {
        var repositoryReader = RepositoryReaderFactory.Create(repositoryUri.AbsoluteUri, httpClient);
        return await repositoryReader.ReadIliSite().ConfigureAwait(false);
    }

    private async Task<InterlisFile?> FetchInterlisFile(Uri fileUri)
    {
        logger.LogDebug("Download INTERLIS file <{URL}>", fileUri);
        try
        {
            var stream = await GetStreamFromUrl(fileUri).ConfigureAwait(false);
            var md5 = await GetMD5FromStream(stream).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            return new InterlisFile
            {
                MD5 = md5,
                Content = content,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException)
        {
            logger.LogError(ex, "Failed to download INTERLIS file <{URL}>", fileUri);
            return null;
        }
    }

    private async Task<Stream> GetStreamFromUrl(Uri url)
    {
        var response = await httpClient.GetAsync(url).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var content = response.Content;
        return await content.ReadAsStreamAsync().ConfigureAwait(false);
    }

    internal static async Task<string> GetMD5FromStream(Stream stream)
    {
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        using var md5 = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
        var hash = await md5.ComputeHashAsync(stream).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private async Task<Uri> PreferHttpsIfAvailable(Uri uri)
    {
        if (Uri.UriSchemeHttps.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            return uri;

        try
        {
            var httpsUri = new Uri(uri.OriginalString.Replace(Uri.UriSchemeHttp, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
            using (var request = new HttpRequestMessage(HttpMethod.Head, httpsUri))
            {
                var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                return response.IsSuccessStatusCode ? httpsUri : uri;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException)
        {
            return uri;
        }
    }

    /// <summary>
    /// Check if the specified <paramref name="url"/> matches any of the URLs in the <paramref name="ignoreList"/>.
    /// </summary>
    private static bool IsIgnored(Uri url, List<Uri> ignoreList)
        => ignoreList.Any(ignoredUri =>
            string.Equals(ignoredUri.Host, url.Host, StringComparison.OrdinalIgnoreCase)
            && AddUrlPathSeparator(url.AbsolutePath).StartsWith(AddUrlPathSeparator(ignoredUri.AbsolutePath), StringComparison.OrdinalIgnoreCase));

    private static string AddUrlPathSeparator(string urlPath)
        => urlPath.EndsWith('/') ? urlPath : urlPath + '/';

    private static Uri GetIlisiteUrl(Uri baseUri) => baseUri.Append("/ilisite.xml");

    private static Uri GetIlimodelsUrl(Uri baseUri) => baseUri.Append("/ilimodels.xml");
}
