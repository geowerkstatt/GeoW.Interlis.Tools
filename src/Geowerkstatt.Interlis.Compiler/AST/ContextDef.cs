namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A context definition (<c>CONTEXT</c>, RefHB 3.8.8/3.10.3). For a named context it fixes which concrete
/// coordinate/scalar domain(s) a generic coordinate domain resolves to.
/// </summary>
public sealed class ContextDef : InterlisDefinition
{
    /// <summary>
    /// The generic-to-concrete coordinate-domain mappings of this context.
    /// </summary>
    public List<ContextMapping> Mappings { get; } = new List<ContextMapping>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitContextDef(this);
    }
}

/// <summary>
/// One <c>GenericCoord-DomainRef '=' Concrete-DomainRef { 'OR' Concrete-DomainRef }</c> mapping of a
/// <see cref="ContextDef"/> (RefHB 3.8.8).
/// </summary>
public sealed class ContextMapping
{
    /// <summary>
    /// The generic coordinate domain being concretized.
    /// </summary>
    public Reference<DomainDef>? GenericCoord { get; set; }

    /// <summary>
    /// The concrete coordinate domain alternatives the generic one resolves to.
    /// </summary>
    public List<Reference<DomainDef>> Concrete { get; } = new List<Reference<DomainDef>>();
}
