namespace Geowerkstatt.Interlis.Tools.AST.Types;

public interface ILineType
{
    public Reference<TypeDef>? VertexType { get; set; }
    public double? OverlapTolerance { get; set; }

    /// <summary>
    /// What kind of segments the line(s) can have e.g. STRAIGHTS, ARCS etc.
    /// </summary>
    public HashSet<string> LineForm { get; }
}
