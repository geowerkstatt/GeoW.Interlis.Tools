using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

public sealed class AttributeDef : IInterlisDefinition, IDocumentation
{
    public required string Name { get; init; }
    public ICollection<RangePosition> NameLocations { get; } = new List<RangePosition>();
    public string FullyQualifiedName => Parent != null ? $"{Parent.FullyQualifiedName} -> {Name}" : Name;
    public IInterlisDefinitionContainer? Parent { get; set; } = null;

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();
    public required TypeDef TypeDef { get; init; }

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitAttributeDef(this);
    }
}
