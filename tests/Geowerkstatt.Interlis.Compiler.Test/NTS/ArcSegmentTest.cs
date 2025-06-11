using NetTopologySuite.Geometries;
using Degrees = NetTopologySuite.Utilities.Degrees;

namespace Geowerkstatt.Interlis.Compiler.NTS;

[TestClass]
public class ArcSegmentTest
{
    [DataTestMethod]
    [DynamicData(nameof(GetCalculateArcTestData), DynamicDataSourceType.Method)]
    public void CalculateArc(Coordinate start, Coordinate end, Coordinate mid, Coordinate expectedCenter, double expectedRadius, int expectedSign)
    {
        (Coordinate actualCenter, double actualRadius, int actualSign) = ArcSegment.CalculateArc(start, end, mid);
        AssertCoordinateEqual(expectedCenter, actualCenter, nameof(expectedCenter));
        Assert.AreEqual(expectedRadius, actualRadius, 1e-12);
        Assert.AreEqual(expectedSign, actualSign);
    }

    public static IEnumerable<object[]> GetCalculateArcTestData()
    {
        var NullCoordinate = new Coordinate(Coordinate.NullOrdinate, Coordinate.NullOrdinate);

        yield return new object[] { new Coordinate(2, 0), new Coordinate(0, 2), new Coordinate(Math.Sqrt(2), Math.Sqrt(2)), new Coordinate(0, 0), 2.0, -1 };
        yield return new object[] { new Coordinate(0, 2), new Coordinate(2, 0), new Coordinate(Math.Sqrt(2), Math.Sqrt(2)), new Coordinate(0, 0), 2.0, 1 };

        // Example from stackoverflow (center not at origin)
        yield return new object[] { new Coordinate(1, 1), new Coordinate(5, 3), new Coordinate(2, 4), new Coordinate(3, 2), Math.Sqrt(5), 1 };
        yield return new object[] { new Coordinate(-1, 5), new Coordinate(1, 3), new Coordinate(-3, 3), new Coordinate(-1, 3), 2.0, -1 };

        // Full circle
        yield return new object[] { new Coordinate(-4, -1), new Coordinate(-4, -1), new Coordinate(2, -1), new Coordinate(-1, -1), 3.0, 0 };

        // Same start and end point
        yield return new object[] { new Coordinate(0, -1), new Coordinate(0, -1), new Coordinate(0, 1), new Coordinate(0, 0), 1.0, 0 };

        // All points same
        yield return new object[] { new Coordinate(42, 5), new Coordinate(42, 5), new Coordinate(42, 5), NullCoordinate, double.PositiveInfinity, 0 };

        // Collinear points
        yield return new object[] { new Coordinate(1, 3), new Coordinate(4, 12), new Coordinate(2, 6), NullCoordinate, double.PositiveInfinity, 0 };
        yield return new object[] { new Coordinate(1, 3), new Coordinate(4, 12), new Coordinate(1, 3), NullCoordinate, double.PositiveInfinity, 0 };
        yield return new object[] { new Coordinate(1, 3), new Coordinate(4, 12), new Coordinate(4, 12), NullCoordinate, double.PositiveInfinity, 0 };

        // Almost collinear
        yield return new object[] { new Coordinate(0, 0), new Coordinate(10, 0), new Coordinate(5, 1e-11), NullCoordinate, double.PositiveInfinity, 0 };
    }

    [DataTestMethod]
    [DynamicData(nameof(GetStrokeArcTestData), DynamicDataSourceType.Method)]
    public void StrokeArc(Coordinate arcStart, Coordinate arcEnd, Coordinate arcMid, double maxError, Coordinate[] expected)
    {
        var arc = new ArcSegment(arcStart, arcEnd, arcMid);
        var actual = arc.StrokeArc(maxError);
        Assert.AreEqual(expected.Length, actual.Length);
        for (int i = 0; i < actual.Length; i++)
        {
            AssertCoordinateEqual(expected[i], actual[i], $"Coordinates at index {i} differ");
        }
    }

    public static IEnumerable<object[]> GetStrokeArcTestData()
    {
        // quarter circle
        yield return new object[]
        {
            new Coordinate(2, 0),
            new Coordinate(0, 2),
            new Coordinate(Math.Sqrt(2), Math.Sqrt(2)),
            0.1,
            new[]
            {
                new Coordinate(2, 0),
                new Coordinate(Math.Cos(Degrees.ToRadians(30)) * 2, Math.Sin(Degrees.ToRadians(30)) * 2),
                new Coordinate(Math.Cos(Degrees.ToRadians(60)) * 2, Math.Sin(Degrees.ToRadians(60)) * 2),
                new Coordinate(0, 2)
            }
        };

        // quarter circle reversed
        yield return new object[]
        {
            new Coordinate(0, 2),
            new Coordinate(2, 0),
            new Coordinate(Math.Sqrt(2), Math.Sqrt(2)),
            0.1,
            new[]
            {
                new Coordinate(0, 2),
                new Coordinate(Math.Cos(Degrees.ToRadians(60)) * 2, Math.Sin(Degrees.ToRadians(60)) * 2),
                new Coordinate(Math.Cos(Degrees.ToRadians(30)) * 2, Math.Sin(Degrees.ToRadians(30)) * 2),
                new Coordinate(2, 0)
            }
        };

        yield return new object[]
        {
            new Coordinate(1, 5),
            new Coordinate(3, 3),
            new Coordinate(-1, 3),
            0.6,
            new[]
            {
                new Coordinate(1, 5),
                new Coordinate(-1, 3),
                new Coordinate(1, 1),
                new Coordinate(3, 3),
            }
        };

        yield return new object[]
        {
            new Coordinate(3, 3),
            new Coordinate(1, 5),
            new Coordinate(-1, 3),
            0.6,
            new[]
            {
                new Coordinate(3, 3),
                new Coordinate(1, 1),
                new Coordinate(-1, 3),
                new Coordinate(1, 5),
            }
        };

        // Full circle
        yield return new object[]
        {
            new Coordinate(-4, -1),
            new Coordinate(-4, -1),
            new Coordinate(2, -1),
            1.0,
            new[]
            {
                new Coordinate(-4, -1),
                new Coordinate(-1, -4),
                new Coordinate(2, -1),
                new Coordinate(-1, 2),
                new Coordinate(-4, -1),
            }
        };

        // Straight line
        yield return new object[]
        {
            new Coordinate(1, 5),
            new Coordinate(6, 3),
            new Coordinate(1, 5),
            0.1,
            new[]
            {
                new Coordinate(1, 5),
                new Coordinate(6, 3),
            }
        };
    }

    [DataTestMethod]
    [DynamicData(nameof(GetExpandEnvelopeTestData), DynamicDataSourceType.Method)]
    public void ExpandEnvelope(Coordinate arcStart, Coordinate arcEnd, Coordinate arcMid, Envelope expectedEnvelope)
    {
        var arc = new ArcSegment(arcStart, arcEnd, arcMid);
        var actualEnvelope = arc.ExpandEnvelope(new Envelope());

        Assert.AreEqual(expectedEnvelope.MinX, actualEnvelope.MinX, 1e-12);
        Assert.AreEqual(expectedEnvelope.MaxX, actualEnvelope.MaxX, 1e-12);
        Assert.AreEqual(expectedEnvelope.MinY, actualEnvelope.MinY, 1e-12);
        Assert.AreEqual(expectedEnvelope.MaxY, actualEnvelope.MaxY, 1e-12);
    }

    public static IEnumerable<object[]> GetExpandEnvelopeTestData()
    {
        yield return new object[] { new Coordinate(1, 5), new Coordinate(4, 2), new Coordinate(2, 3), new Envelope(1, 4, 2, 5) };
        yield return new object[] { new Coordinate(1, 1), new Coordinate(5, 3), new Coordinate(2, 4), new Envelope(3 - Math.Sqrt(5), 5, 1, 2 + Math.Sqrt(5)) };

        // Straight line
        yield return new object[] { new Coordinate(1, 2), new Coordinate(4, 8), new Coordinate(2, 4), new Envelope(1, 4, 2, 8) };

        // Full circle
        yield return new object[] { new Coordinate(1, 2), new Coordinate(1, 2), new Coordinate(3, 2), new Envelope(1, 3, 1, 3) };
    }

    /// <summary>
    /// Compare coordinates with a tolerance
    /// </summary>
    private static void AssertCoordinateEqual(Coordinate expected, Coordinate actual, string message=null)
    {
        Assert.IsTrue((!expected.IsValid && !actual.IsValid) || expected.Equals2D(actual, 1e-12), $"Expected coordinate {expected} but got {actual}{(message != null ? ". " + message : "")}");
    }
}
