namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class BooleanType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }
}
