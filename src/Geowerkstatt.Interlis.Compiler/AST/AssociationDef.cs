using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST;

public class AssociationDef : IInterlisDefinitionContainer, IExtending<AssociationDef>, IIdentifiable
{
    public required string Name { get; init; }
    public IInterlisDefinitionContainer? Parent { get; set; } = null;

    public Reference<AssociationDef>? Extends { get; set; }

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    /// <inheritdoc />
    public Dictionary<string, AssociationDef> AssociationAccess { get; } = new Dictionary<string, AssociationDef>();

    public required Cardinality Cardinality { get; init; }

    public Reference<TypeDef>? OidType { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitAssociationDef(this);
    }
}
