namespace Geowerkstatt.Interlis.Compiler.AST;

public sealed class ModelDef : IDocumentation, IInterlisDefinitionContainer
{
    public required string Name { get; init; }
    public ICollection<RangePosition> NameLocations { get; } = new List<RangePosition>();
    public IInterlisDefinitionContainer? Parent { get; set; }

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public IList<(bool IsUnqualifiedAllowed, Reference<ModelDef> ModelDef)> Imports { get; } = new List<(bool, Reference<ModelDef>)>();

    public string? Language { get; set; }
    public string? URI { get; set; }
    public string? Version { get; set; }
    public string? Xmlns { get; set; }

    public ICollection<IReference> ContainerReferences { get; } = new List<IReference>();

    /// <summary>
    /// The path or URL to the source interlis file where this model was defined.
    /// </summary>
    public string? SourceUri { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitModelDef(this);
    }
}
