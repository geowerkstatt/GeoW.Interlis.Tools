namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Base class for INTERLIS definitions. It carries the <see cref="Name"/>, <see cref="NameLocations"/>,
/// <see cref="Parent"/>, <see cref="DocComments"/>, <see cref="MetaAttributes"/> and <see cref="SourceRange"/> state shared by every
/// <see cref="IInterlisDefinition"/>, so concrete definitions only declare their distinctive members.
/// Subclasses implement <see cref="Accept{TResult}"/> to dispatch to their specific visitor method.
/// </summary>
public abstract class InterlisDefinition : IInterlisDefinition
{
    public required string Name { get; init; }

    public ICollection<RangePosition> NameLocations { get; } = new List<RangePosition>();

    public IInterlisDefinitionContainer? Parent { get; set; }

    public Reference<IInterlisDefinition>? TranslationOf { get; set; }

    /// <summary>
    /// The fully qualified name. Concrete (not just the <see cref="IInterlisDefinition"/> default) and
    /// <see langword="virtual"/> so a subclass with a different scheme (e.g. <see cref="AttributeDef"/>) can
    /// override it and have that override observed through the <see cref="IInterlisDefinition"/> interface.
    /// </summary>
    public virtual string FullyQualifiedName => Parent != null ? $"{Parent.FullyQualifiedName}.{Name}" : Name;

    public IList<string> DocComments { get; } = new List<string>();

    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public RangePosition? SourceRange { get; init; }

    public abstract TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor);
}
