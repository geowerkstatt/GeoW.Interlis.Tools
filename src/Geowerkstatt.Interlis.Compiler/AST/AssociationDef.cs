namespace Geowerkstatt.Interlis.Tools.AST;

public class AssociationDef : IAstElement, IInterlisDefinitionContainer
{
    public required string Name { get; init; }
    public IInterlisDefinitionContainer? Parent { get; set; } = null;

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public required Cardinality Cardinality { get; init; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitAssociationDef(this);
    }
}
