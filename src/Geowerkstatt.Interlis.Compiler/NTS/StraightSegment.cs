using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Tools.NTS;

public class StraightSegment : LineSegment, ICurveSegment
{
    public StraightSegment(Coordinate p0, Coordinate p1) : base(p0, p1)
    {
    }

    public Coordinate[] GetCoordinates(double maxError) => [P0, P1];

    public Envelope ExpandEnvelope(Envelope envelope)
    {
        envelope.ExpandToInclude(P0);
        envelope.ExpandToInclude(P1);

        return envelope;
    }

    public override bool Equals(object? obj) 
        =>  obj is StraightSegment segment && Equals(segment);

    public bool Equals(ICurveSegment? obj)
        => obj is StraightSegment segment && Equals(segment);

    public bool Equals(StraightSegment other) 
        => (EqualityComparer<Coordinate>.Default.Equals(P1, other.P1) && EqualityComparer<Coordinate>.Default.Equals(P0, other.P0))
        || (EqualityComparer<Coordinate>.Default.Equals(P0, other.P1) && EqualityComparer<Coordinate>.Default.Equals(P1, other.P0));

    public override int GetHashCode()
    {
        var (smaller, bigger) = P0.CompareTo(P1) < 0 ? (P0, P1) : (P1, P0);
        return HashCode.Combine(smaller, bigger);
    }


    public Coordinate Start => P0;

    public Coordinate End => P1;


}
