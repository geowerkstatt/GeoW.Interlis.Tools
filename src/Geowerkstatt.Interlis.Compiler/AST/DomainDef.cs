using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A type definition that has a name and can therefore be referenced.
/// </summary>
public class DomainDef : IInterlisDefinition, IDocumentation
{
    public required string Name { get; init; }
    public ICollection<RangePosition> NameLocations { get; } = new List<RangePosition>();
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
