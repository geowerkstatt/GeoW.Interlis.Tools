namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class NumericType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    public double? Min { get; set; }
    public double? Max { get; set; }
    public int? Precision { get; set; }

    public bool Circular { get; set; }
}
