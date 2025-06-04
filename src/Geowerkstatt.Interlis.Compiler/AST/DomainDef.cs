using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST;

/// <summary>
/// A type definition that has a name and can therefore be referenced.
/// </summary>
public class DomainDef : IInterlisDefinition, IDocumentation
{
    public required string Name { get; init; }
    public IInterlisDefinitionContainer? Parent { get; set; }

    public required TypeDef TypeDef { get; init; }

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitDomainDef(this);
    }
}
