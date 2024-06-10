using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Tools.NTS;

public interface ICurveSegment
{
    /// <summary>
    /// Expand the given <paramref name="envelope"/> to include this segment.
    /// </summary>
    public Envelope ExpandEnvelope(Envelope envelope);

    /// <summary>
    /// A list of <see cref="Coordinate"/>s that, when connected by straight lines represent or approximate this segment.
    /// </summary>
    public Coordinate[] Coordinates { get; }

    public Coordinate Start { get; }

    public Coordinate End { get; }
}
