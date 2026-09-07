namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Base class for the consistency constraints of a class, structure or association (RefHB 3.12).
/// A constraint may carry an optional name (used for messages); it is not part of the namespace.
/// Each concrete constraint type dispatches to its own visitor method via <see cref="Accept{TResult}"/>.
/// </summary>
public abstract class ConstraintDef : IInterlisDefinition
{
    private readonly string? ExplicitName;

    /// <summary>
    /// The 1-based position of this constraint among all constraints (named and anonymous) of its container, in
    /// source order; <c>0</c> when unset (e.g. domain constraints). Used to synthesize the implicit <see cref="Name"/>
    /// of an anonymous constraint, mirroring ili2c's per-container <c>constraintIdx</c> counter.
    /// </summary>
    internal int NameIndex { get; set; }

    /// <summary>
    /// The constraint name used in diagnostic messages. Returns the explicit <c>Constraint-Name ':'</c> if one was
    /// given, otherwise a <c>!!@ name</c> meta-attribute, otherwise the synthesized <c>Constraint&lt;NameIndex&gt;</c>
    /// (e.g. <c>Constraint1</c>). Mirrors ili2c's <c>Constraint.getName()</c>. A constraint is not part of the
    /// namespace; the name is used for messages only, so it is always non-empty.
    /// </summary>
    public string Name
    {
        get => !string.IsNullOrEmpty(ExplicitName) ? ExplicitName
            : MetaAttributes.TryGetValue("name", out var metaName) ? metaName
            : $"Constraint{NameIndex}";
        init => ExplicitName = value;
    }

    public ICollection<RangePosition> NameLocations { get; } = new List<RangePosition>();

    public IInterlisDefinitionContainer? Parent { get; set; }

    public Reference<IInterlisDefinition>? TranslationOf { get; set; }

    public IList<string> DocComments { get; } = new List<string>();

    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public RangePosition? SourceRange { get; init; }

    public abstract TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor);
}
