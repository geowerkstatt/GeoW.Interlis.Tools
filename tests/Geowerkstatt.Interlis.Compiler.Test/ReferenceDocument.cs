namespace Geowerkstatt.Interlis.Compiler.Test;

/// <summary>
/// A standard document converted to anchored HTML (see the docs folder) that
/// test cases can link into via section numbers or paragraph anchors.
/// </summary>
public sealed class ReferenceDocument
{
    /// <summary>eCH-0031 INTERLIS 2-Referenzhandbuch (INTERLIS 2.4).</summary>
    public static readonly ReferenceDocument RefHB = new(Path.Combine("docs", "interlis2-referenzhandbuch.html"), "RefHB");

    /// <summary>eCH-0117 Meta-Attribute für INTERLIS-Modelle.</summary>
    public static readonly ReferenceDocument Ech0117 = new(Path.Combine("docs", "ech0117-meta-attribute.html"), "eCH-0117");

    private readonly string displayName;
    private readonly Lazy<string?> documentPath;

    private ReferenceDocument(string fileName, string displayName)
    {
        this.displayName = displayName;
        documentPath = new Lazy<string?>(() => FindDocument(fileName));
    }

    /// <summary>
    /// Builds a clickable file link into the document. Accepts section numbers
    /// ("3.2.3"), section-relative paragraph anchors ("3.2.3-2") or raw anchor names
    /// ("anhang-A-2", "page-53"). Returns a plain text reference if the document
    /// file is not found next to or above the test directory.
    /// </summary>
    public string Link(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return reference;
        }

        var anchor = char.IsAsciiDigit(reference[0]) ? $"s{reference}" : reference;
        var path = documentPath.Value;
        return path is null
            ? $"{displayName} {reference}"
            : $"{new Uri(path).AbsoluteUri}#{anchor}";
    }

    private static string? FindDocument(string fileName)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
