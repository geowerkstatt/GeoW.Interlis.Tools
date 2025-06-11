using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.XtfReader.NTS;

[TestClass]
public class StraigthSegmentTest
{
    [TestMethod]
    public void Equals_ReturnsTrueOnSameObject()
    {
        var segment = new StraightSegment(new Coordinate(1, 1), new Coordinate(0, 0));

        Assert.IsTrue(segment.Equals(segment));
        Assert.IsTrue(((ICurveSegment)segment).Equals(segment));
        Assert.IsTrue(((object)segment).Equals(segment));
    }

    [TestMethod]
    public void Equals_ShouldFailOnNull()
    {
        var segment = new StraightSegment(new Coordinate(1, 1), new Coordinate(0, 0));

        Assert.IsFalse(((ICurveSegment)segment).Equals(null));
        Assert.IsFalse(((object)segment!).Equals(null));
    }

    [TestMethod]
    public void Eqals_ShoudReturnTrueOnEquivalentSegment()
    {
        var left = new StraightSegment(new Coordinate(1, 2), new Coordinate(3, 4));
        var right = new StraightSegment(new Coordinate(1, 2), new Coordinate(3, 4));

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(((ICurveSegment)left).Equals(right));
        Assert.IsTrue(((object)left).Equals(right));
    }

    [TestMethod]
    public void Eqals_ShoudReturnTrueOnMirroredSegment()
    {
        var left = new StraightSegment(new Coordinate(1, 2), new Coordinate(3, 4));
        var right = new StraightSegment(new Coordinate(3, 4), new Coordinate(1, 2));

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(((ICurveSegment)left).Equals(right));
        Assert.IsTrue(((object)left).Equals(right));
    }

    [TestMethod]
    public void Eqals_ShoudReturnFailOnSingleMismatch()
    {
        var left = new StraightSegment(new Coordinate(1, 2), new Coordinate(3, 4));
        var rights = new StraightSegment[]
        {
            new StraightSegment(new Coordinate(0, 2), new Coordinate(3, 4)),
            new StraightSegment(new Coordinate(1, 0), new Coordinate(3, 4)),
            new StraightSegment(new Coordinate(1, 2), new Coordinate(0, 4)),
            new StraightSegment(new Coordinate(1, 2), new Coordinate(3, 0)),
            new StraightSegment(new Coordinate(0, 4), new Coordinate(1, 2)),
            new StraightSegment(new Coordinate(3, 0), new Coordinate(1, 2)),
            new StraightSegment(new Coordinate(3, 4), new Coordinate(0, 2)),
            new StraightSegment(new Coordinate(3, 4), new Coordinate(1, 0)),
        };

        foreach (var right in rights)
        {
            Assert.IsFalse(left.Equals(right));
            Assert.IsFalse(((ICurveSegment)left).Equals(right));
            Assert.IsFalse(((object)left).Equals(right));
        }
    }

    [TestMethod]
    public void GetHashCode_ShouldHandleMirroredSegments()
    {
        var left = new StraightSegment(new Coordinate(1, 2), new Coordinate(3, 4));
        var right = new StraightSegment(new Coordinate(3, 4), new Coordinate(1, 2));

        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }
}
