namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class SurfaceType : ITypeDef, ILineType
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    public bool IsMultiGeometry { get; set; }
    public bool IsCoverage { get; set; }

    public ITypeDef? VertexType { get; set; }
    public double? OverlapTolerance { get; set; }

    public HashSet<string> LineForm { get; } = new HashSet<string>();
}
