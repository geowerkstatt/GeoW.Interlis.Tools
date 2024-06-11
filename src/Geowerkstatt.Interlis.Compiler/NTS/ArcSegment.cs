using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Tools.NTS;

[Serializable]
public class ArcSegment : ICurveSegment
{
    private const double Epsilon = 1e-8;

    public Coordinate Start { get; }
    public Coordinate End { get; }
    public Coordinate Mid { get; }

    public Coordinate Center { get; }
    public double Radius { get; }

    /// <summary>
    /// The sign of the arc.
    /// <c>1</c> if the arc is clockwise, <c>-1</c> if the arc is counter-clockwise, <c>0</c> if the arc is a straight line or closed.
    /// </summary>
    public int Sign { get; }

    private Coordinate[]? coordinates;

    public Coordinate[] GetCoordinates(double maxError)
    {
        if (coordinates == null)
        {
            coordinates = StrokeArc(maxError);
        }

        return coordinates;
    }

    /// <summary>
    /// Construct an <see cref="ArcSegment"/> from a start-point to an end-point that passes through the mid-point.
    /// </summary>
    public ArcSegment(Coordinate start, Coordinate end, Coordinate mid)
    {
        Start = start;
        End = end;
        Mid = mid;
        (Center, Radius, Sign) = CalculateArc(Start, End, Mid);
    }

    public bool IsStraightLine()
    {
        return !double.IsFinite(Radius);
    }

    public bool IsFullCircle()
    {
        return Start.Equals2D(End) && Center.IsValid;
    }

    /// <summary>
    /// Calculate center and radius of the arc.
    /// Based on <see href="https://math.stackexchange.com/q/1460096">Get the equation of a circle when given 3 points</see>,
    /// <see href="https://en.wikipedia.org/wiki/Rule_of_Sarrus">determinant of a 3x3 matrix</see>.
    /// </summary>
    public static (Coordinate center, double radius, int sign) CalculateArc(Coordinate start, Coordinate end, Coordinate mid)
    {
        double m11 = (start.X * mid.Y) + (mid.X * end.Y) + (end.X * start.Y) - (start.X * end.Y) - (mid.X * start.Y) - (end.X * mid.Y);

        if (Math.Abs(m11) < Epsilon)
        {
            if (start.Equals2D(end) && !start.Equals2D(mid))
            {
                // Special case full circle, mid defines the opposite side of the circle
                var center = new Coordinate(start.X + ((mid.X - start.X) / 2.0), start.Y + ((mid.Y - start.Y) / 2.0));
                var radius = center.Distance(start);
                var sign = 0;
                return (center, radius, sign);
            }
            else
            {
                // Points are collinear or almost collinear
                return (new(Coordinate.NullOrdinate, Coordinate.NullOrdinate), double.PositiveInfinity, 0);
            }
        }
        else
        {
            double a21 = Math.Pow(start.X, 2) + Math.Pow(start.Y, 2);
            double a31 = Math.Pow(mid.X, 2) + Math.Pow(mid.Y, 2);
            double a41 = Math.Pow(end.X, 2) + Math.Pow(end.Y, 2);

            double m12 = (a21 * mid.Y) + (a31 * end.Y) + (a41 * start.Y) - (a21 * end.Y) - (a31 * start.Y) - (a41 * mid.Y);
            double m13 = (a21 * mid.X) + (a31 * end.X) + (a41 * start.X) - (a21 * end.X) - (a31 * start.X) - (a41 * mid.X);

            var center = new Coordinate((m12 / m11) / 2.0, (m13 / m11) / -2.0);
            var radius = center.Distance(start);
            var sign = (int)Orientation.Index(start, end, mid);
            return (center, radius, sign);
        }
    }

    /// <summary>
    /// Calculate a collection of <see cref="Coordinate"/>s that define a <see cref="LineString"/> that approximates the arc.
    /// </summary>
    /// <remarks>The mid-point of the arc may not lay on an approximated line.</remarks>
    /// <param name="arc">The <see cref="ArcSegment"/> to stroke.</param>
    /// <param name="maxError">The straight lines that approximate the circle are at most <paramref name="maxError"/> from the circle away.</param>
    public Coordinate[] StrokeArc(double maxError)
    {
        if (IsStraightLine())
        {
            return [Start, End];
        }

        // Maximum central angle between two points to still satisfy maxError
        var maxTheta = 2 * Math.Acos(1 - (maxError / Radius));

        var startAngle = Math.Atan2(Start.Y - Center.Y, Start.X - Center.X);
        var endAngle = Math.Atan2(End.Y - Center.Y, End.X - Center.X);

        // Modify endAngle to make it larger/smaller than startAngle depending on the sign
        var sign = Sign == 0 ? 1 : -Sign;
        if (sign > 0 && endAngle <= startAngle) endAngle += Math.Tau;
        if (sign < 0 && endAngle > startAngle) endAngle -= Math.Tau;

        var totalAngle = Math.Abs(endAngle - startAngle);

        var count = (int)Math.Ceiling(totalAngle / maxTheta);
        var theta = totalAngle / count * sign;

        // Copy start and end points unchanged to avoid imprecisions
        var result = new Coordinate[count + 1];
        result[0] = new Coordinate(Start);
        for (int i = 1; i < count; i++)
        {
            var angle = startAngle + (i * theta);
            var x = Center.X + Math.Cos(angle) * Radius;
            var y = Center.Y + Math.Sin(angle) * Radius;
            result[i] = new Coordinate(x, y);
        }

        result[count] = new Coordinate(End);
        return result;
    }

    /// <summary>
    /// <inheritdoc />
    /// Based on <see href="https://stackoverflow.com/a/77799448"/>.
    /// </summary>
    public Envelope ExpandEnvelope(Envelope envelope)
    {
        envelope.ExpandToInclude(Start);
        envelope.ExpandToInclude(End);

        if (!IsStraightLine())
        {
            // Vector from Start (a) to End (b)
            var ab = (x: End.X - Start.X, y: End.Y - Start.Y);

            // Z-Part of cross-product `ab x (Mid - a)`.
            // Also equal to sin(theta) where theta is the angle between vector `ab` and vector `(Mid - a)`.
            // The sign of this expression tells on which side of `ab` `(Mid - a)` is.
            var m = ab.x * (Start.Y - Mid.Y) - ab.y * (Start.X - Mid.X);

            // Include all circle bounding points that are on the same side of `ab` as the mid-point.
            var circlePoints = new[]
            {
                (x: Center.X, y: Center.Y + Radius),
                (x: Center.X, y: Center.Y - Radius),
                (x: Center.X + Radius, y: Center.Y),
                (x: Center.X - Radius, y: Center.Y),
            };
            foreach (var point in circlePoints)
            {
                var p = ab.x * (Start.Y - point.y) - ab.y * (Start.X - point.x);
                if (p * m >= 0)
                {
                    envelope.ExpandToInclude(point.x, point.y);
                }
            }
        }

        return envelope;
    }

    /// <summary>
    /// Returns the Well-Known Text (WKT) representation of this <see cref="ArcSegment"/>.
    /// </summary>
    public override string ToString() => $"CIRCULARSTRING({Start.X} {Start.Y}, {Mid.X} {Mid.Y}, {End.X} {End.Y})";
}
