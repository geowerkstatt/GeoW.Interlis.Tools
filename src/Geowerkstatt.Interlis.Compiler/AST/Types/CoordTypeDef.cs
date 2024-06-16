namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class CoordTypeDef : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    public bool IsMultiGeometry { get; set; }

    public List<NumericTypeDef> Axis { get; } = new List<NumericTypeDef>();
}
