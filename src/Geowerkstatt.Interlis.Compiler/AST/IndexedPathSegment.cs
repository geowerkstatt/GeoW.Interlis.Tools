namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An indexed attribute step of an object path (<c>Attribute-Name '[' FIRST | LAST | PosNumber ']'</c>, RefHB 3.13).
/// The index is mandatory: its presence proves the step denotes an attribute (an ordered <c>LIST OF</c> sub-structure
/// or a coordinate), so — unlike a bare <see cref="PathSegment"/>, which could denote a role, base name or reference
/// attribute — this step is unambiguously an attribute. The span covers the name; the bracket is not a name.
/// </summary>
public sealed class IndexedPathSegment : PathSegment
{
    /// <summary>The element or axis the step selects.</summary>
    public required PathIndex Index { get; init; }

    public override string ToString() => $"{Name}[{Index}]";
}
