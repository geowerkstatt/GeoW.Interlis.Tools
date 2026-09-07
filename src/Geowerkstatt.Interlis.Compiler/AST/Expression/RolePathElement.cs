namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// A role step of an object path qualified by its association (<c>Role-Name '[' Association-Name ']'</c>, RefHB 3.13).
/// The <see cref="AssociationName"/> narrows which of the class's association accesses ("Beziehungszugang", RefHB 2.7)
/// the role belongs to. It is a plain name — resolved against the class's association accesses (topic-local), not the
/// general namespace — so it deliberately is not a <see cref="Reference{T}"/> (a scoped reference could resolve to an
/// unqualified-imported association from another topic that grants no access). An unqualified role is a bare
/// <see cref="IdentifierPathElement"/>.
/// </summary>
public sealed class RolePathElement : IPathElement
{
    public required string Name { get; init; }

    public required string AssociationName { get; init; }
}
