
namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// An enumeration value that can have sub-enumerations
/// </summary>
public class EnumerationTreeNode : IDocumentation
{
    public required string Name { get; init; }

    public EnumerationValuesList SubValues { get; } = new EnumerationValuesList();

    /// <summary>
    /// Whether this element's name was written after a dot in a dotted element name: in
    /// <c>rot.dunkelrot (...)</c> every name after the first carries the flag. A dotted name builds the same
    /// nested tree as authored nesting (<c>rot (dunkelrot (...))</c>), but it is only allowed to identify an
    /// inherited enumeration element that an extension refines (RefHB 3.8.2-17), so the type checker rejects
    /// dotted names in primary definitions, without a sub-enumeration, or naming no inherited element. The flag
    /// on every continuation keeps the two notations distinguishable (<c>rot.a.b</c> vs. <c>rot (a.b)</c>),
    /// which the per-nesting uniqueness of authored element names (RefHB 3.8.2-5) depends on.
    /// </summary>
    public bool FromDottedName { get; init; }

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();
}
