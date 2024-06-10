using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools.NTS;
using NetTopologySuite.Geometries;
using System.Xml;
using System.Xml.Linq;

namespace Geowerkstatt.Interlis.Tools.XtfReader;

public class XtfReader
{
    private const string InterlisNamespace = "http://www.interlis.ch/xtf/2.4/INTERLIS";
    private const string GeometryNamespace = "http://www.interlis.ch/geometry/1.0";
    private const string DefaultModelNamespacePrefix = "http://www.interlis.ch/xtf/2.4/";

    private GeometryFactory factory = new GeometryFactory();

    public InterlisFile? InterlisEnvironment { get; set; }

    public IEnumerable<InterlisObject> ReadXtf(TextReader textReader)
    {
        using (var reader = XmlReader.Create(textReader))
        {
            reader.EnsureIsStartElement("transfer", InterlisNamespace);

            // Ignore header section

            reader.ReadToDescendant("datasection", InterlisNamespace);
            if (reader.ReadToDescendant())
            {
                foreach (var obj in ReadBasket(reader.ReadSubtree()))
                {
                    yield return obj; 
                }

                while (reader.ReadToNextSibling())
                {
                    foreach (var obj in ReadBasket(reader.ReadSubtree()))
                    {
                        yield return obj;   
                    }
                }
            }
        }
    }

    private IEnumerable<InterlisObject> ReadBasket(XmlReader reader)
    {
        reader.Read();
        var modelNamespace = reader.NamespaceURI;
        var topic = reader.LocalName;

        //InterlisEnvironment.Content.Values.Where(m => m.)

        reader.MoveToFirstAttribute();
        // reader.NamespaceURI == InterlisNamespace
        // reader.LocalName == "bid"

        var basket = new InterlisBasket
        {
            Bid = reader.Value,
        };

        while (reader.ReadToNextSibling())
        {
            var obj = ReadObject(reader.ReadSubtree());
            obj.Basket = basket;
            yield return obj;
        }
    }

    private InterlisObject ReadObject(XmlReader reader)
    {
        reader.Read();
        var element = (XElement)XNode.ReadFrom(reader);
        var attributes = element.Elements().Select(ReadAttribute).ToDictionary();

        return new InterlisObject
        {
            Tid = element.Attributes().WhereName(InterlisNamespace, "tid").Single().Value,
            Attributes = { attributes },
        };
    }

    private (string name, object value) ReadAttribute(XElement attributeData)
    {
        var name = attributeData.Name.LocalName;

        if (attributeData.HasElements)
        {
            var content = attributeData.Elements().First().Name.LocalName;
            Func<XElement, object> readValue = (content) switch
            {
                "surface" => ReadSurface,
                _ => throw new NotImplementedException($"Attributes of type '{content}' are not supported"),
            };

            var values = attributeData.Elements().Select(readValue).ToArray();

            return (name, values.Length == 1 ? values[0] : values.ToList());
        }

        var refAttribute = attributeData.Attributes().WhereName(InterlisNamespace, "ref").SingleOrDefault();

        if (refAttribute != null)
        {
            return (name, refAttribute.Value);
        }
        
        return (name, attributeData.Value);
    }

    private Coordinate ReadCoordinate(XElement coord)
    {
        var hasC1 = double.TryParse(coord.Elements().WhereName(GeometryNamespace, "c1").SingleOrDefault()?.Value, out double c1);
        var hasC2 = double.TryParse(coord.Elements().WhereName(GeometryNamespace, "c2").SingleOrDefault()?.Value, out double c2);
        var hasC3 = double.TryParse(coord.Elements().WhereName(GeometryNamespace, "c3").SingleOrDefault()?.Value, out double c3);

        if (!hasC1 || !hasC2)
        {
            throw new ArgumentException("C1 and C2 are required");
        }

        if (hasC3)
        {
            return new CoordinateZ(c1, c2, c3);
        }
        else
        {
            return new Coordinate(c1, c2);
        }
    }

    private Point ReadPoint(XElement coord)
    {
        return new Point(ReadCoordinate(coord));
    }

    private MultiPoint ReadMultiCoord(XElement multiCoord)
    {
        var points = multiCoord.Elements()
            .Select(ReadCoordinate)
            .Select(factory.CreatePoint)
            .ToArray();

        return new MultiPoint(points, factory);
    }

    private ArcSegment ReadArcSegment(XElement arc, Coordinate start)
    {
        var hasC1 = double.TryParse(arc.Elements().WhereName(GeometryNamespace, "c1").SingleOrDefault()?.Value, out double c1);
        var hasC2 = double.TryParse(arc.Elements().WhereName(GeometryNamespace, "c2").SingleOrDefault()?.Value, out double c2);
        var hasC3 = double.TryParse(arc.Elements().WhereName(GeometryNamespace, "c3").SingleOrDefault()?.Value, out double c3);
        var hasA1 = double.TryParse(arc.Elements().WhereName(GeometryNamespace, "a1").SingleOrDefault()?.Value, out double a1);
        var hasA2 = double.TryParse(arc.Elements().WhereName(GeometryNamespace, "a2").SingleOrDefault()?.Value, out double a2);
        var hasR = double.TryParse(arc.Elements().WhereName(GeometryNamespace, "r").SingleOrDefault()?.Value, out double r);

        if (hasR) throw new ArgumentException("R not supported");
        if (!hasC1 || !hasC2 || !hasA1 || !hasA2) throw new ArgumentException("C1, C2, A1 and A2 are required");

        var end = hasC3 ? new CoordinateZ(c1, c2, c3) : new Coordinate(c1, c2);
        var mid = new Coordinate(a1, a2);

        return new ArcSegment(start, end, mid);
    }

    private StraightSegment ReadStraightSegment(XElement coord, Coordinate start)
    {
        var end = ReadCoordinate(coord);
        return new StraightSegment(start, end);
    }

    private CompoundCurve ReadPolyline(XElement polyline)
    {
        var firstElement = polyline.Elements().First();
        if (firstElement.Name.LocalName != "coord")
        {
            throw new ArgumentException("First element of polyline must be 'coord'");
        }

        var previousCoord = ReadCoordinate(firstElement);

        var segments = new List<ICurveSegment>();
        foreach ( var element in polyline.Elements().Skip(1) )
        {
            ICurveSegment segment = (element.Name.LocalName) switch
            {
                "coord" => ReadStraightSegment(element, previousCoord),
                "arc" => ReadArcSegment(element, previousCoord),
                _ => throw new ArgumentException($"Unsupported line type {element.Name}"),
            };

            segments.Add(segment);
            previousCoord = segment.End;
        }

        return new CompoundCurve(segments, factory);
    }

    private CurvePolygon ReadSurface(XElement surface)
    {
        var shell = surface
            .Elements()
            .WhereName(GeometryNamespace, "exterior")
            .Single()
            .Elements()
            .WhereName(GeometryNamespace, "polyline")
            .Select(s => new CompoundCurveRing(ReadPolyline(s)))
            .Single();

        var holes = surface
            .Elements()
            .WhereName(GeometryNamespace, "interior")
            .Select(e => e
                .Elements()
                .WhereName(GeometryNamespace, "polyline")
                .Single())
            .Select(h => new CompoundCurveRing(ReadPolyline(h)))
            .ToArray();

        return new CurvePolygon(shell, holes, factory);
    }

    private CurveMultiPolygon ReadMultiSurface(XElement surface)
    {
        var surfaces = surface.Elements().Select(ReadSurface).ToArray();
        return new CurveMultiPolygon(surfaces, factory);
    }

    private void PrintWithDepth(XmlReader reader)
    {
        while(reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Whitespace:
                    break;
                default:
                    Console.WriteLine($"{string.Concat(Enumerable.Repeat("|  ", reader.Depth))}[{reader.Depth}] {reader.NodeType} ({reader.LocalName}) xmlns={reader.NamespaceURI}");
                    break;
            }

        }
    }
}
