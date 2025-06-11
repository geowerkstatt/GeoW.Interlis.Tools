namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class CoordType : TypeDef
{
    public bool IsMultiGeometry { get; set; }

    public List<NumericType> Axis { get; } = new List<NumericType>();
}
