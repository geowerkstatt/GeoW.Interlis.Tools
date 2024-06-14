namespace Geowerkstatt.Interlis.Tools.AST;

public class TextTypeDef : ITypeDef
{
    public Cardinality? Cardinality { get; set; }

    public int? Length { get; set; }
}
