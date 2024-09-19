using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST;

public class AssociationDef : IAstElement, IInterlisDefinitionContainer, IExtending<AssociationDef>
{
    public required string Name { get; init; }
    public IInterlisDefinitionContainer? Parent { get; set; } = null;

    public AssociationDef? Extends { get; set; }

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public required Cardinality Cardinality { get; init; }

    public TypeDef? OidType { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitAssociationDef(this);
    }
}
