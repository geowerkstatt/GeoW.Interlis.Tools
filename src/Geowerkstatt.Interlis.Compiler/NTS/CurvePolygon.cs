using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Tools.NTS;

public class CurvePolygon
{
    public CompoundCurveRing Shell { get; }

    public CompoundCurveRing[] Holes { get; }

    public GeometryFactory Factory { get; }

    public object UserData { get; set; }

    public CurvePolygon(CompoundCurveRing shell, CompoundCurveRing[] holes, GeometryFactory factory)
    {
        Shell = shell;
        Holes = holes;
        Factory = factory;
    }

    public static explicit operator Polygon(CurvePolygon curvePolygon)
    {
        return curvePolygon.ConvertToPolygon(0.01);
    }

    public Polygon ConvertToPolygon(double maxError)
    {
        return new Polygon(Shell.ConvertToLinearRing(maxError), Holes.Select(h => h.ConvertToLinearRing(maxError)).ToArray(), Factory);
    }

    /// <summary>
    /// Returns the Well-Known Text (WKT) representation of this <see cref="CurvePolygon"/>.
    /// </summary>
    public override string ToString()
    {
        if (Holes.Any())
        {
            var holesWkt = string.Join(",", Holes.Select(s => s.ToString()));
            return $"CURVEPOLYGON({Shell}, {holesWkt})";
        }
        else
        {
            return $"CURVEPOLYGON({Shell})";
        }
    }
}
