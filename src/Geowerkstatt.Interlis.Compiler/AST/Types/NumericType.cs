namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class NumericType : TypeDef
{
    public double? Min { get; set; }
    public double? Max { get; set; }
    public int? Precision { get; set; }

    public bool Circular { get; set; }
    public Reference<UnitDef>? Unit { get; set; }
}
