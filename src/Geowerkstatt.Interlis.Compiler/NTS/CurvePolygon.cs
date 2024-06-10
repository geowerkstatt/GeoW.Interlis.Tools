using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.NTS;

public class CurvePolygon
{
    public CompoundCurveRing Shell { get; }

    public CompoundCurveRing[] Holes { get; }

    public GeometryFactory Factory { get; }

    public CurvePolygon(CompoundCurveRing shell, CompoundCurveRing[] holes, GeometryFactory factory)
    {
        Shell = shell;
        Holes = holes;
        Factory = factory;
    }

    public static explicit operator Polygon(CurvePolygon curvePolygon)
    {
        return new Polygon((LinearRing)curvePolygon.Shell, curvePolygon.Holes.Select(h => (LinearRing)h).ToArray(), curvePolygon.Factory);
    }
}
