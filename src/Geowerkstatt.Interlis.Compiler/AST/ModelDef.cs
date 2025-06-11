namespace Geowerkstatt.Interlis.Compiler.AST;

public sealed class ModelDef : IDocumentation, IInterlisDefinitionContainer
{
    public required string Name { get; init; }
    public IInterlisDefinitionContainer? Parent { get; set; }

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public IDictionary<string, (bool IsUnqualifiedAllowed, ModelDef? ModelDef)> Imports { get; } = new Dictionary<string, (bool, ModelDef?)>();

    public string? Language { get; set; }
    public string? URI { get; set; }
    public string? Version { get; set; }
    public string? Xmlns { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitModelDef(this);
    }
}
