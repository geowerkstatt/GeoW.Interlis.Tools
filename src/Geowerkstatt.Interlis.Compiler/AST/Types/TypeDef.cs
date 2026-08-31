namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public abstract class TypeDef : IExtending<DomainDef>, ISourceRange
{
    public Cardinality? Cardinality { get; set; }
    public Reference<DomainDef>? Extends { get; set; }

    /// <summary>
    /// The value restrictions of the type (<c>CONSTRAINTS</c>, RefHB 3.8-8), keyed by their name — every
    /// restriction has a unique name within its domain definition, so a duplicate is unrepresentable (the build
    /// visitor reports and drops it).
    /// </summary>
    public Dictionary<string, DomainConstraint> Constraints { get; } = new Dictionary<string, DomainConstraint>();

    public RangePosition? SourceRange { get; init; }

    /// <summary>
    /// The concrete type this type stands for: a pure alias (<see cref="TypeRef"/>, which references a domain
    /// without changing anything) is followed — transitively, guarding against extension cycles — to the referenced
    /// domain's type. Any other node is its own underlying type, including one that extends a domain but narrows it
    /// locally. The last alias reached is returned when its target is unresolved.
    /// <para>
    /// Deliberately internal: this answers only what KIND of value the type holds — the alias's and the
    /// intermediate bases' own <see cref="Cardinality"/> and <see cref="Constraints"/> are NOT merged into the
    /// result, so handing this to AST consumers would invite misreading it as the full effective type. A
    /// consumer-facing accessor would have to collapse the whole extension chain (strongest cardinality,
    /// accumulated constraints, merged enumeration refinements) into one self-contained type.
    /// </para>
    /// </summary>
    internal TypeDef Underlying()
    {
        var visited = new HashSet<TypeDef>();
        var current = this;
        while (current is TypeRef && visited.Add(current) && current.Extends?.Target?.TypeDef is { } aliased)
        {
            current = aliased;
        }

        return current;
    }

    /// <summary>
    /// Applies this type as an extension on top of the effective (already merged) type of its base chain and
    /// returns the new effective type: definition parts this type declares override the base's, omitted parts are
    /// inherited (RefHB 3.8-4 only recommends repeating unchanged parts, so omission means inheritance). Folding a
    /// chain root-first with this yields the effective type of the whole chain — the state each extension must be
    /// checked against, and eventually the consumer-facing flattened type.
    /// <para>
    /// The merge is total: it never throws and never rejects — on a kind clash it keeps the authored value (the
    /// type checker owns reporting illegal extensions). The result is read-only and never part of the AST: it is
    /// either an existing node reused unchanged or a fresh synthetic node without <see cref="Extends"/>,
    /// <see cref="SourceRange"/>, <see cref="Cardinality"/> or <see cref="Constraints"/>. Use-site cardinality
    /// and the chain's accumulated <see cref="Constraints"/> (every restriction of the chain applies,
    /// RefHB 3.8-8 "gelten alle") are deliberately NOT merged here: reused nodes would need detached copies to
    /// carry them — chain-level checks walk the authored base chain instead, and the flattening accessor will
    /// combine them on its always-fresh nodes.
    /// </para>
    /// <para>
    /// The base implementation returns the extension unchanged (nothing inheritable is modelled for the kind);
    /// kinds with inheritable definition parts override it.
    /// </para>
    /// </summary>
    /// <param name="effectiveBase">The effective type of the base chain this type extends.</param>
    internal virtual TypeDef MergeWithBase(TypeDef effectiveBase) => this;
}
