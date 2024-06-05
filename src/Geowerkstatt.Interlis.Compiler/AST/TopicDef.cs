namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class TopicDef : IAstElement, IInterlisDefinition, IDocumentation, IContainer<IInterlisDefinition>
{
    public required string Name { get; init; }
    public IInterlisDefinition? Parent { get; set; } = null;

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitTopicDef(this);
    }
}
