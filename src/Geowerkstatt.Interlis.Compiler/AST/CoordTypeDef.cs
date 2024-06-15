namespace Geowerkstatt.Interlis.Tools.AST;

public class CoordTypeDef : ITypeDef
{
    public Cardinality? Cardinality { get; set; }

    public bool IsMultiGeometry { get; set; }

    public List<NumericTypeDef> Axis { get; } = new List<NumericTypeDef>();
}
