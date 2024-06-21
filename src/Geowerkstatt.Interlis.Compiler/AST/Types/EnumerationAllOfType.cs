namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class EnumerationAllOfType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    public EnumerationType? TargetEnumeration { get; set; }
}
