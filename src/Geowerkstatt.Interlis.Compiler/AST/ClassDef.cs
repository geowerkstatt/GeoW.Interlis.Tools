using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class ClassDef : IDocumentation, IExtending<ClassDef>, IInterlisDefinitionContainer, IIdentifiable
{
    public required string Name { get; init; }
    public IInterlisDefinitionContainer? Parent { get; set; }

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public Reference<ClassDef>? Extends { get; set; }

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    /// <inheritdoc />
    public Dictionary<string, AssociationDef> AssociationAccess { get; } = new Dictionary<string, AssociationDef>();

    public bool IsStructure { get; init; }

    public Reference<TypeDef>? OidType { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitClassDef(this);
    }
}
