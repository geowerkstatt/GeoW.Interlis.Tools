using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Compiler.NTS;

public class CurveMultiPolygon
{
    public CurvePolygon[] Polygons { get; }

    public GeometryFactory Factory { get; }

    public object? UserData { get; set; }

    public CurveMultiPolygon(CurvePolygon[] polygons, GeometryFactory factory)
    {
        Polygons = polygons;
        Factory = factory;
    }

    public static explicit operator MultiPolygon(CurveMultiPolygon curvePolygon)
    {
        return curvePolygon.ConvertToMultiPolygon(0.01);
    }

    public MultiPolygon ConvertToMultiPolygon(double maxError)
    {
        return new MultiPolygon(Polygons.Select(p => p.ConvertToPolygon(maxError)).ToArray(), Factory);
    }

    /// <summary>
    /// Returns the Well-Known Text (WKT) representation of this <see cref="CurveMultiPolygon"/>.
    /// </summary>
    public override string ToString()
    {
        var curvePolyWkt = string.Join(",", Polygons.Select(s => s.ToString()));
        return $"GEOMETRYCOLLECTION({curvePolyWkt})";
    
    }
}
