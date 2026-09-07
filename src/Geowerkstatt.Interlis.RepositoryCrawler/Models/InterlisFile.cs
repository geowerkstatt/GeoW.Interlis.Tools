using System.ComponentModel.DataAnnotations;

namespace Geowerkstatt.Interlis.RepositoryCrawler.Models;

public class InterlisFile
{
    /// <summary>
    /// The MD5 hash of the file content. This is the cache key, so a file mirrored across several repositories
    /// (byte-identical content) is stored only once. Which URL a model resolves to is tracked separately by
    /// <see cref="InterlisFileReference"/>, so cached content is never served for a URL it was not fetched from.
    /// </summary>
    [Key]
    public required string MD5 { get; set; }

    /// <summary>The content of the INTERLIS file.</summary>
    public required string Content { get; set; }
}
