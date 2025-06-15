using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

public sealed class TopicDef : IDocumentation, IExtending<TopicDef>, IInterlisDefinitionContainer
{
    public required string Name { get; init; }
    public ICollection<RangePosition> NameLocations { get; } = new List<RangePosition>();
    public IInterlisDefinitionContainer? Parent { get; set; } = null;

    public Reference<TopicDef>? Extends { get; set; }

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public Reference<TypeDef>? BasketOidType { get; set; }
    public Reference<TypeDef>? OidType { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitTopicDef(this);
    }
}
