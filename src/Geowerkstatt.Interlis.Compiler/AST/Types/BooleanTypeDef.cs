namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class BooleanTypeDef : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }
}
