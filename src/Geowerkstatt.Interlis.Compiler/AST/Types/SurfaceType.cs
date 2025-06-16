namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class SurfaceType : TypeDef, ILineType
{
    public bool IsMultiGeometry { get; set; }
    public bool IsCoverage { get; set; }

    public Reference<DomainDef>? VertexType { get; set; }
    public double? OverlapTolerance { get; set; }

    public HashSet<string> LineForm { get; } = new HashSet<string>();
}
