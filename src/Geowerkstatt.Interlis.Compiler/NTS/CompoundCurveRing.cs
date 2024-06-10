using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Tools.NTS;

/// <summary>
/// A <see cref="CompoundCurve"/> with the guarantee that the first and last point are equal.
/// </summary>
public class CompoundCurveRing : CompoundCurve
{
    public CompoundCurveRing(List<ICurveSegment> segments, GeometryFactory factory)
        : base(segments, factory)
    {
        if (!IsClosed())
        {
            throw new ArgumentException("Segments must form a closed ring", nameof(segments));
        }
    }

    public CompoundCurveRing(CompoundCurve compoundCurve) : this(compoundCurve.Segments, compoundCurve.Factory)
    {
    }

    public static explicit operator LinearRing(CompoundCurveRing compoundCurveRing)
    {
        return compoundCurveRing.ConvertToLinearRing(0.01);
    }

    public LinearRing ConvertToLinearRing(double maxError)
    {
        return new LinearRing(DerivePoints(Segments, Factory, maxError), Factory);
    }
}
