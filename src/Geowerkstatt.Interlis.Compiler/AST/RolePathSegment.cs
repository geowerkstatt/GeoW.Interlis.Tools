namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A role step of an object path qualified by its association (<c>Role-Name '[' Association-Name ']'</c>, RefHB 3.13).
/// The <see cref="Association"/> narrows which of the class's association accesses ("Beziehungszugang", RefHB 2.7)
/// the role belongs to. Both names are looked up in those accesses rather than the general namespace: a scoped
/// lookup of the association could reach an unqualified-imported one from another topic that grants no access.
/// The qualifier is a second written name with its own span and target, but not a step of the path: a consumer
/// collecting every name a reference writes has to look inside this segment. An unqualified role is a bare
/// <see cref="PathSegment"/>.
/// </summary>
public sealed class RolePathSegment : PathSegment
{
    /// <summary>The association named inside the brackets.</summary>
    public required PathSegment Association { get; init; }

    public override string ToString() => $"{Name}[{Association.Name}]";
}
