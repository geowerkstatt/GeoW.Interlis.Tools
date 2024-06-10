using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Tools.NTS;

public class CompoundCurve
{
    public List<ICurveSegment> Segments { get; }
    public GeometryFactory Factory { get; }

    public CompoundCurve(List<ICurveSegment> segments, GeometryFactory factory)
    {
        Segments = segments;
        Factory = factory;
    }

    public bool IsClosed()
    {
        if (Segments.Count == 0) return false;

        return Segments.First().Start.Equals2D(Segments.Last().End);
    }

    public Envelope ComputeEnvelopeInternal()
    {
        var envelope = new Envelope();

        foreach (var segment in Segments)
        {
            segment.ExpandEnvelope(envelope);
        }

        return envelope;
    }

    protected static CoordinateSequence DerivePoints(List<ICurveSegment> segments, GeometryFactory factory)
    {
        CoordinateList result = new CoordinateList();
        foreach (var segment in segments)
        {
            result.Add(segment.Coordinates, allowRepeated: false);
        }

        return factory.CoordinateSequenceFactory.Create(result.ToArray());
    }

    public static explicit operator LineString(CompoundCurve compoundCurve)
    {
        return new LineString(DerivePoints(compoundCurve.Segments, compoundCurve.Factory), compoundCurve.Factory);
    }
}
