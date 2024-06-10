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
        return curvePolygon.ConvertToMultiPolygon(0.01);
    }

    public MultiPolygon ConvertToMultiPolygon(double maxError)
    {
        return new MultiPolygon(Polygons.Select(p => p.ConvertToPolygon(maxError)).ToArray(), Factory);
    }
}
