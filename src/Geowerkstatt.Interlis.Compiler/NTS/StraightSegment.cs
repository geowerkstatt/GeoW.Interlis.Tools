using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.NTS;

public class StraightSegment : LineSegment, ICurveSegment
{
    public StraightSegment(Coordinate p0, Coordinate p1) : base(p0, p1)
    {
    }

    public Coordinate[] Coordinates => [P0, P1];

    public Envelope ExpandEnvelope(Envelope envelope)
    {
        envelope.ExpandToInclude(P0);
        envelope.ExpandToInclude(P1);

        return envelope;
    }

    public Coordinate Start => P0;

    public Coordinate End => P1;
}
