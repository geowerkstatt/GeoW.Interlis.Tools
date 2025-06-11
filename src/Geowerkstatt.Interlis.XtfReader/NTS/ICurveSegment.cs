using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.XtfReader.NTS;

public interface ICurveSegment : IEquatable<ICurveSegment>
{
    /// <summary>
    /// Expand the given <paramref name="envelope"/> to include this segment.
    /// </summary>
    public Envelope ExpandEnvelope(Envelope envelope);

    /// <summary>
    /// A list of <see cref="Coordinate"/>s that, when connected by straight lines represent or approximate this segment.
    /// </summary>
    public Coordinate[] GetCoordinates(double maxError = 0.01);

    public Coordinate Start { get; }

    public Coordinate End { get; }
}
