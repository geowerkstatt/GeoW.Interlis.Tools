namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class SurfaceType : TypeDef, ILineType
{
    public bool IsMultiGeometry { get; set; }
    public bool IsCoverage { get; set; }

    public Reference<DomainDef>? VertexType { get; set; }
    public WithoutOverlapsDef? WithoutOverlaps { get; set; }

    public List<Reference<LineFormTypeDef>> LineForms { get; } = new List<Reference<LineFormTypeDef>>();

    /// <summary>
    /// A surface/area extension inherits the definition parts it omits (RefHB 3.8-4): the line form and the
    /// vertex declaration, and the overlap tolerance, which can not be overridden anyway (RefHB 3.8.12.2-23). A
    /// SURFACE may be extended to an AREA (RefHB 3.8.13.4-3), so the coverage flag is kept once set.
    /// </summary>
    internal override TypeDef MergeWithBase(TypeDef effectiveBase)
    {
        if (effectiveBase is not SurfaceType baseSurface)
        {
            return this;
        }

        var merged = new SurfaceType
        {
            IsMultiGeometry = IsMultiGeometry || baseSurface.IsMultiGeometry,
            IsCoverage = IsCoverage || baseSurface.IsCoverage,
            VertexType = VertexType ?? baseSurface.VertexType,
            WithoutOverlaps = WithoutOverlaps is WithoutOverlapsDef.Explicit ? WithoutOverlaps : baseSurface.WithoutOverlaps ?? WithoutOverlaps,
        };
        merged.LineForms.AddRange(LineForms.Count > 0 ? LineForms : baseSurface.LineForms);
        return merged;
    }
}
