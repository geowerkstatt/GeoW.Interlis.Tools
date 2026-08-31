namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class PolyLineType : TypeDef, ILineType
{
    public bool IsMultiGeometry { get; set; }
    public bool IsDirected { get; set; }

    public Reference<DomainDef>? VertexType { get; set; }
    public WithoutOverlapsDef? WithoutOverlaps { get; set; }

    public List<Reference<LineFormTypeDef>> LineForms { get; } = new List<Reference<LineFormTypeDef>>();

    /// <summary>
    /// A line extension inherits the definition parts it omits (RefHB 3.8-4): the line form and the vertex
    /// declaration (e.g. <c>DirectedLine EXTENDS Line = DIRECTED POLYLINE;</c> adds only the direction,
    /// RefHB 3.8.12.2-23), and the overlap tolerance, which can not be overridden anyway (RefHB 3.8.12.2-23).
    /// </summary>
    internal override TypeDef MergeWithBase(TypeDef effectiveBase)
    {
        if (effectiveBase is not PolyLineType baseLine)
        {
            return this;
        }

        var merged = new PolyLineType
        {
            IsMultiGeometry = IsMultiGeometry || baseLine.IsMultiGeometry,
            IsDirected = IsDirected || baseLine.IsDirected,
            VertexType = VertexType ?? baseLine.VertexType,
            WithoutOverlaps = WithoutOverlaps is WithoutOverlapsDef.Explicit ? WithoutOverlaps : baseLine.WithoutOverlaps ?? WithoutOverlaps,
        };
        merged.LineForms.AddRange(LineForms.Count > 0 ? LineForms : baseLine.LineForms);
        return merged;
    }
}
