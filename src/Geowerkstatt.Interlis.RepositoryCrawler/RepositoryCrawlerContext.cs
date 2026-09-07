using Geowerkstatt.Interlis.RepositoryCrawler.Models;
using Microsoft.EntityFrameworkCore;

namespace Geowerkstatt.Interlis.RepositoryCrawler;

/// <summary>
/// Represents the database context for the INTERLIS repository crawler cache.
/// </summary>
internal class RepositoryCrawlerContext : DbContext
{
    public DbSet<Repository> Repositories { get; set; }
    public DbSet<Model> Models { get; set; }
    public DbSet<Catalog> Catalogs { get; set; }
    public DbSet<InterlisFile> InterlisFiles { get; set; }
    public DbSet<InterlisFileReference> InterlisFileReferences { get; set; }
    public DbSet<CrawlInformation> CrawlInformations { get; set; }

    public RepositoryCrawlerContext(DbContextOptions<RepositoryCrawlerContext> options)
        : base(options)
    {
    }
}
