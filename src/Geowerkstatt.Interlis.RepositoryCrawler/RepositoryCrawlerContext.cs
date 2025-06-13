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

    //private static string dbDirectory = Path.Combine(Path.GetTempPath(), "Geowerkstatt.Interlis");
    //private static string dbFileName = $"ModelRepositoryCache{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? ""}.db";
    //public string DbPath { get; } = Path.Combine(dbDirectory, dbFileName);

    public RepositoryCrawlerContext(DbContextOptions<RepositoryCrawlerContext> options)
        : base(options)
    {
    }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    Directory.CreateDirectory(dbDirectory);
    //    optionsBuilder.UseSqlite($"Data Source={DbPath}");
    //}
}
