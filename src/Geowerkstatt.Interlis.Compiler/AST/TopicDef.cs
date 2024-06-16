using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class TopicDef : IAstElement, IInterlisDefinition, IDocumentation, IContainer<IInterlisDefinition>, IExtending<TopicDef>
{
    public required string Name { get; init; }
    public IInterlisDefinition? Parent { get; set; } = null;

    public TopicDef? Extends { get; set; }

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public ITypeDef? BasketOidType { get; set; }
    public ITypeDef? OidType { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitTopicDef(this);
    }
}
