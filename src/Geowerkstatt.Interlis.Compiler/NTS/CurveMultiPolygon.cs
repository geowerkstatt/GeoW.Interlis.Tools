using NetTopologySuite.Geometries;

namespace Geowerkstatt.Interlis.Tools.NTS;

public class CurveMultiPolygon
{
    public CurvePolygon[] Polygons { get; }

    public GeometryFactory Factory { get; }

    public CurveMultiPolygon(CurvePolygon[] polygons, GeometryFactory factory)
    {
        Polygons = polygons;
        Factory = factory;
    }

    public static explicit operator MultiPolygon(CurveMultiPolygon curvePolygon)
    {
        return new MultiPolygon(curvePolygon.Polygons.Select(p => (Polygon)p).ToArray(), curvePolygon.Factory);
    }
}
