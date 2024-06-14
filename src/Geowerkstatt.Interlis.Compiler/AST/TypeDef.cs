namespace Geowerkstatt.Interlis.Tools.AST;

public class TypeDef : ITypeDef
{
    /// <summary>
    /// Interlis-syntax string for now
    /// </summary>
    public string? Definition { get; init; }

    public required Cardinality? Cardinality { get; set; }
}
