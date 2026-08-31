namespace Geowerkstatt.Interlis.Compiler.AST;

public sealed class TopicDef : InterlisDefinition, IExtending<TopicDef>, IInterlisDefinitionContainer
{
    public Reference<TopicDef>? Extends { get; set; }

    /// <summary>
    /// Whether the topic is declared as a <c>VIEW TOPIC</c> (RefHB 3.5.2-7).
    /// </summary>
    public bool IsView { get; set; }

    /// <summary>
    /// The topics this topic <c>DEPENDS ON</c> (RefHB 3.5.2-7).
    /// </summary>
    public List<Reference<TopicDef>> DependsOn { get; } = new List<Reference<TopicDef>>();

    /// <summary>
    /// The generic coordinate domains listed after <c>DEFERRED GENERICS</c> (RefHB 3.5.2-7/11).
    /// </summary>
    public List<Reference<DomainDef>> DeferredGenerics { get; } = new List<Reference<DomainDef>>();

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();


    public Reference<DomainDef>? BasketOidType { get; set; }
    public Reference<DomainDef>? OidType { get; set; }

    public ICollection<IReference> ContainerReferences { get; } = new List<IReference>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitTopicDef(this);
    }
}
