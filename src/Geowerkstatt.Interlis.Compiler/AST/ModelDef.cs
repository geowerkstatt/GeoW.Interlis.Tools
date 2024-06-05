namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class ModelDef : IAstElement, IInterlisDefinition, IDocumentation, IContainer<IInterlisDefinition>
{
    public required string Name { get; init; }
    public IInterlisDefinition? Parent { get; set; }

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public string? Language { get; set; }
    public string? URI { get; set; }
    public string? Version { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitModelDef(this);
    }
}
