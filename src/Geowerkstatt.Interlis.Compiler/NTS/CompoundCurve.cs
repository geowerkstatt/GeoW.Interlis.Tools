using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Tools.NTS;

public class CompoundCurve
{
    public List<ICurveSegment> Segments { get; }
    public GeometryFactory Factory { get; }

    public object? UserData { get; set; }

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

    protected static CoordinateSequence DerivePoints(List<ICurveSegment> segments, GeometryFactory factory, double maxError)
    {
        CoordinateList result = new CoordinateList();
        foreach (var segment in segments)
        {
            result.Add(segment.GetCoordinates(maxError), allowRepeated: false);
        }

        return factory.CoordinateSequenceFactory.Create(result.ToArray());
    }

    public static explicit operator LineString(CompoundCurve compoundCurve)
    {
        return compoundCurve.ConvertToLineString(0.01);
    }

    public LineString ConvertToLineString(double maxError)
    {
        return new LineString(DerivePoints(Segments, Factory, maxError), Factory);
    }

    /// <summary>
    /// Returns the Well-Known Text (WKT) representation of this <see cref="CompoundCurve"/>.
    /// </summary>
    public override string ToString()
    {
        var segmentsWkt = string.Join(",", Segments.Select(s => s.ToString()));
        return $"COMPOUNDCURVE({segmentsWkt})";
    }
}
