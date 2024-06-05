namespace Geowerkstatt.Interlis.Tools.AST;

public class TypeDef : ITypeDef, IInterlisDefinition
{
    public required string Name { get; init; }
    public IInterlisDefinition? Parent { get; set; } = null;

    /// <summary>
    /// Interlis-syntax string for now
    /// </summary>
    public string? Definition { get; init; }

    public Cardinality? Cardinality { get; set; }
}
