namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A reference attribute (<c>REFERENCE TO</c>, RefHB 3.6.3): a pointer to an independent object of the referenced
/// class. A substructure reference (<c>attr : Struct</c> / <c>BAG OF</c> / <c>ANYSTRUCTURE</c>) is a
/// <see cref="ObjectType"/> (contained substructure) instead, and a domain reference (<c>attr : Domain</c>) is a <see cref="TypeRef"/>.
/// </summary>
public class ReferenceType : TypeDef
{
    public required RestrictedRef Target { get; init; }
    public HashSet<Property> Properties { get; } = new HashSet<Property>();
}
