namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// The format definition of a <c>FORMAT BASED ON</c> formatted value domain (RefHB 3.8.6-6).
/// An ordered sequence of literal separators and base-attribute references, optionally extending an
/// inherited format (<c>INHERITANCE</c>).
/// </summary>
public class FormatDef
{
    /// <summary>
    /// Whether the format starts with <c>INHERITANCE</c>, i.e. extends the inherited format (RefHB 3.8.6-10).
    /// </summary>
    public bool Inheritance { get; set; }

    /// <summary>
    /// The format components in source order (literal separators and base-attribute references).
    /// </summary>
    public List<IFormatComponent> Components { get; } = new List<IFormatComponent>();
}

/// <summary>
/// A single element of a <see cref="FormatDef"/>: either a <see cref="FormatSeparator"/> or a
/// <see cref="FormatBaseAttribute"/>.
/// </summary>
public interface IFormatComponent
{
}

/// <summary>
/// A literal separator string (<c>NonNum-String</c>) inside a format definition.
/// </summary>
public sealed class FormatSeparator : IFormatComponent
{
    public required string Value { get; init; }
}

/// <summary>
/// A base-attribute reference inside a format definition (RefHB 3.8.6-7). Either a numeric attribute with
/// an optional integer-position count, or a structure attribute with a referenced formatted domain.
/// </summary>
public sealed class FormatBaseAttribute : IFormatComponent
{
    /// <summary>
    /// The referenced attribute within the base structure. Resolves as <see cref="ReferenceResolution.Member"/> —
    /// the attribute is a member of the <c>BASED ON</c> structure, not a scoped name — so the reference resolver
    /// sets the target via member lookup.
    /// </summary>
    public required Reference<AttributeDef> Attribute { get; init; }

    /// <summary>
    /// For a numeric attribute: the optional integer-position count (<c>name '/' IntPos</c>).
    /// </summary>
    public int? Position { get; init; }

    /// <summary>
    /// For a structure attribute: the referenced formatted domain (<c>name '/' Formatted-DomainRef</c>).
    /// </summary>
    public Reference<DomainDef>? FormattedDomain { get; init; }
}
