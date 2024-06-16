namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class TextTypeDef : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    public int? Length { get; set; }
}
