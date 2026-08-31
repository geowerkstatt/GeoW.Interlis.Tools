using System.ComponentModel.DataAnnotations;

namespace Geowerkstatt.Interlis.RepositoryCrawler.Models;

/// <summary>
/// Maps a source URL to the MD5 of the content last fetched from it. This is the cache lookup key: content is only
/// ever reused for the exact URL it was fetched from, so a wrong or copy-pasted MD5 hash in a repository catalog can
/// never cause another file's content to be served in its place. Several URLs that serve byte-identical content point
/// at the same <see cref="InterlisFile"/> (referenced by <see cref="MD5"/>), so identical files are still stored once.
/// </summary>
public class InterlisFileReference
{
    /// <summary>The URL the content was downloaded from.</summary>
    [Key]
    public required string SourceUrl { get; set; }

    /// <summary>The <see cref="InterlisFile.MD5"/> of the content last fetched from <see cref="SourceUrl"/>.</summary>
    public required string MD5 { get; set; }
}
