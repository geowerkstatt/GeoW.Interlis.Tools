namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Defines how many objects are applicable and if they are ordered. A pure multiplicity value — what it counts
/// depends on where it sits: the size of an owned value on attribute types, the link population per source object
/// on a <see cref="Types.RoleType"/> (RefHB 3.7.3), the size of the denoted set on a computed
/// <see cref="Types.ObjectType"/>.
/// </summary>
public sealed record Cardinality
{
    public static readonly long? Unbound = null;

    public required long? Min { get; init; } = Unbound;
    public required long? Max { get; init; } = Unbound;
    public bool Ordered { get; init; } = false;
}
