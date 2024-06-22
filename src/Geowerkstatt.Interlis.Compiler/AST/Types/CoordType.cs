namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class CoordType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    public bool IsMultiGeometry { get; set; }

    public List<NumericType> Axis { get; } = new List<NumericType>();
}
