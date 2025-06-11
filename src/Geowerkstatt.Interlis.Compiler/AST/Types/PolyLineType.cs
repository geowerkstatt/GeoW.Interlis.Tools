namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class PolyLineType : TypeDef, ILineType
{
    public bool IsMultiGeometry { get; set; }
    public bool IsDirected { get; set; }

    public Reference<TypeDef>? VertexType { get; set; }
    public double? OverlapTolerance { get; set; }

    public HashSet<string> LineForm { get; } = new HashSet<string>();
}
