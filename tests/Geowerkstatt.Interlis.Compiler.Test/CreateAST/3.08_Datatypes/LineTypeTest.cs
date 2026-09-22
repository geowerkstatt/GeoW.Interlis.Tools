using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class LineTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Abstract polyline without line form or vertex",
            "Attr : POLYLINE;",
            Description: "Abstract line range: line form and vertex range may be omitted (a bare POLYLINE).",
            RefHB: "3.8.12.2-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 15),
                },
            }));

        yield return FullFile(new(
            "Line extension reduces curve form",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS Base =
                        Geom : POLYLINE WITH (STRAIGHTS, ARCS) VERTEX CoordD;
                    END Base;
                    CLASS Sub EXTENDS Base =
                        Geom (EXTENDED) : POLYLINE WITH (STRAIGHTS) VERTEX CoordD;
                    END Sub;
                END Topic;
            END Model.
            """,
            Description: "Extensibility: the curve form may be reduced (ARCS dropped), not extended, in an attribute extension.",
            RefHB: "3.8.12.2-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Line extension restricts coordinate range",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordWide = COORD 0.000 .. 100.000, 0.000 .. 100.000;
                    CoordNarrow EXTENDS CoordWide = COORD 0.000 .. 50.000, 0.000 .. 50.000;
                TOPIC Topic =
                    CLASS Base =
                        Geom : POLYLINE WITH (STRAIGHTS) VERTEX CoordWide;
                    END Base;
                    CLASS Sub EXTENDS Base =
                        Geom (EXTENDED) : POLYLINE WITH (STRAIGHTS) VERTEX CoordNarrow;
                    END Sub;
                END Topic;
            END Model.
            """,
            Description: "Extensibility: the coordinate range of an extended line must be a restriction of the base range.",
            RefHB: "3.8.12.2-3",
            AssertOutput: false));

        yield return FullFile(new(
            "Line extension inheriting form and vertex is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                    Line (ABSTRACT) = POLYLINE WITH (STRAIGHTS, ARCS) VERTEX CoordD;
                    DirectedLine EXTENDS Line = DIRECTED POLYLINE;
            END Model.
            """,
            Description: """
                A line extension inherits the omitted line form and vertex declaration (RefHB 3.8-4), so a concrete
                extension adding only the direction is complete through its base and needs no ABSTRACT declaration. This
                is the CHBase Geometry_V2 pattern (Line/DirectedLine).
                """,
            RefHB: "3.8.12.2-23",
            AssertOutput: false));

        yield return FullFile(new(
            "Area extension inheriting form and vertex is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                    Surface (ABSTRACT) = SURFACE WITH (STRAIGHTS, ARCS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    Territory EXTENDS Surface = AREA;
            END Model.
            """,
            Description: """
                The same inheritance turning a SURFACE into an AREA, with the overlap tolerance declared on the base (it
                can not be overridden anyway, RefHB 3.8.12.2-23).
                """,
            RefHB: "3.8.13.4-3",
            AssertOutput: false));

        yield return Rule(new(
            "Polyline without overlaps with explicit tolerance",
            "Attr : POLYLINE WITH (STRAIGHTS, ARCS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;",
            Description: "Explicit overlap tolerance on a single polyline: a decimal > 0 follows WITHOUT OVERLAPS >.",
            RefHB: "3.8.12.2-16",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    WithoutOverlaps = new WithoutOverlapsDef.Explicit { Tolerance = 0.005 },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 77),
                },
            }));

        yield return Rule(new(
            "Polyline without overlaps",
            "Attr : POLYLINE VERTEX CoordD WITHOUT OVERLAPS;",
            Description: "Single polyline overlap-freedom: WITHOUT OVERLAPS may be requested on a POLYLINE.",
            RefHB: "3.8.12.2-19",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    WithoutOverlaps = new WithoutOverlapsDef.Implicit(),
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 46),
                },
            }));

        yield return Rule(new(
            "Surface without explicit overlaps clause",
            "Attr : SURFACE WITH (STRAIGHTS) VERTEX CoordD;",
            Description: "Overlap-freedom is mandatory for SURFACE, so WITHOUT OVERLAPS may be omitted.",
            RefHB: "3.8.12.2-22",
            Ili2cDivergenceReason: "ili2c requires an explicit WITHOUT OVERLAPS on surface and area types and rejects the omission as incomplete; RefHB 3.8.12.2-22 explicitly permits omitting it (overlap-freedom is mandatory for surfaces and area partitions anyway, and the tolerance is implicit or inherited per 3.8.13.1-13), so we accept.",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new SurfaceType
                {
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 45),
                },
            }));

        yield return FullFile(new(
            "Undirected polyline extended to directed",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS Base =
                        Geom : POLYLINE WITH (STRAIGHTS) VERTEX CoordD;
                    END Base;
                    CLASS Sub EXTENDS Base =
                        Geom (EXTENDED) : DIRECTED POLYLINE WITH (STRAIGHTS) VERTEX CoordD;
                    END Sub;
                END Topic;
            END Model.
            """,
            Description: "3.8.12.2-24 Extensibility: an undirected polyline may be extended to a directed polyline.",
            RefHB: "3.8.12.2-23",
            AssertOutput: false));

        yield return FullFile(new(
            "Extended attribute completes an inherited line type",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS Base (ABSTRACT) =
                        Geom : POLYLINE WITH (STRAIGHTS) VERTEX CoordD;
                    END Base;
                    CLASS Sub EXTENDS Base =
                        Geom (EXTENDED) : DIRECTED POLYLINE;
                    END Sub;
                END Topic;
            END Model.
            """,
            Description: """
                Like a domain extension (DirectedLine EXTENDS Line = DIRECTED POLYLINE, RefHB 3.8.12.2-23), the
                extended attribute's line type adds only the direction and inherits the line form and vertex
                declaration from the inherited attribute — it is complete through its base, so neither the
                attribute nor its class needs an ABSTRACT declaration.
                """,
            RefHB: "3.8.12.2-23",
            AssertOutput: false));

        yield return FullFile(new(
            "Line on abstract coordinate in abstract structure",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    AbstractCoord (ABSTRACT) = COORD NUMERIC, NUMERIC;
                TOPIC Topic =
                    STRUCTURE LineStruct (ABSTRACT) =
                        Geom : POLYLINE WITH (STRAIGHTS) VERTEX AbstractCoord;
                    END LineStruct;
                END Topic;
            END Model.
            """,
            Description: """
                If the vertex coordinate type is abstract, the line must itself be declared abstract — the abstract
                structure alone is not enough, the attribute carrying the line type must be ABSTRACT too.
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.LineStruct -> Geom' at 7:12-7:66: must be declared ABSTRACT because its type is not fully defined."],
            RefHB: "3.8.12.2-26",
            AssertOutput: false));

        yield return Rule(new(
            "Directed polyline",
            "Attr : DIRECTED POLYLINE WITH (STRAIGHTS, ARCS) VERTEX CoordD;",
            Description: "LineType alternatives: [DIRECTED] POLYLINE.",
            RefHB: "3.8.12.2-28",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    IsDirected = true,
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 61),
                },
            }));

        yield return Rule(new(
            "Multipolyline",
            "Attr : MULTIPOLYLINE WITH (STRAIGHTS, ARCS) VERTEX CoordD;",
            Description: "LineType alternatives: MULTIPOLYLINE.",
            RefHB: "3.8.12.2-28",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    IsMultiGeometry = true,
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 57),
                },
            }));

        yield return Rule(new(
            "Directed multipolyline",
            "Attr : DIRECTED MULTIPOLYLINE WITH (STRAIGHTS, ARCS) VERTEX CoordD;",
            Description: "LineType alternatives: DIRECTED MULTIPOLYLINE.",
            RefHB: "3.8.12.2-28",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    IsMultiGeometry = true,
                    IsDirected = true,
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 66),
                },
            }));

        yield return Rule(new(
            "Surface with overlap tolerance",
            "Attr : SURFACE WITH (STRAIGHTS, ARCS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;",
            Description: "LineType alternatives: SURFACE (single surface) with explicit overlap tolerance.",
            RefHB: "3.8.12.2-28",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new SurfaceType
                {
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    WithoutOverlaps = new WithoutOverlapsDef.Explicit { Tolerance = 0.005 },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 76),
                },
            }));

        yield return Rule(new(
            "Area",
            "Attr : AREA WITH (STRAIGHTS, ARCS) VERTEX CoordD;",
            Description: "LineType alternatives: AREA (area partition).",
            RefHB: "3.8.12.2-28",
            Ili2cDivergenceReason: "ili2c requires an explicit WITHOUT OVERLAPS on surface and area types and rejects the omission as incomplete; RefHB 3.8.12.2-22 explicitly permits omitting it (overlap-freedom is mandatory for surfaces and area partitions anyway, and the tolerance is implicit or inherited per 3.8.13.1-13), so we accept.",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new SurfaceType
                {
                    IsCoverage = true,
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 48),
                },
            }));

        yield return Rule(new(
            "Multisurface",
            "Attr : MULTISURFACE WITH (STRAIGHTS, ARCS) VERTEX CoordD;",
            Description: "LineType alternatives: MULTISURFACE.",
            RefHB: "3.8.12.2-28",
            Ili2cDivergenceReason: "ili2c requires an explicit WITHOUT OVERLAPS on surface and area types and rejects the omission as incomplete; RefHB 3.8.12.2-22 explicitly permits omitting it (overlap-freedom is mandatory for surfaces and area partitions anyway, and the tolerance is implicit or inherited per 3.8.13.1-13), so we accept.",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new SurfaceType
                {
                    IsMultiGeometry = true,
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 56),
                },
            }));

        yield return Rule(new(
            "Multiarea",
            "Attr : MULTIAREA WITH (STRAIGHTS, ARCS) VERTEX CoordD;",
            Description: "LineType alternatives: MULTIAREA.",
            RefHB: "3.8.12.2-28",
            Ili2cDivergenceReason: "ili2c requires an explicit WITHOUT OVERLAPS on surface and area types and rejects the omission as incomplete; RefHB 3.8.12.2-22 explicitly permits omitting it (overlap-freedom is mandatory for surfaces and area partitions anyway, and the tolerance is implicit or inherited per 3.8.13.1-13), so we accept.",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new SurfaceType
                {
                    IsMultiGeometry = true,
                    IsCoverage = true,
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 53),
                },
            }));

        yield return Rule(new(
            "Polyline with line forms and vertex",
            "Attr : POLYLINE WITH (STRAIGHTS, ARCS) VERTEX CoordD;",
            RefHB: "3.8.12.2-29",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } } },
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 52),
                },
            }));

        yield return Rule(new(
            "Polyline with straights only",
            "Attr : POLYLINE WITH (STRAIGHTS) VERTEX CoordD WITHOUT OVERLAPS > 0.010;",
            Description: "3.8.12.2-30 LineForm with a single LineFormType (STRAIGHTS only).",
            RefHB: "3.8.12.2-29",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    WithoutOverlaps = new WithoutOverlapsDef.Explicit { Tolerance = 0.01 },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 71),
                },
            }));

        yield return Rule(new(
            "Polyline with model-qualified line form",
            "Attr : POLYLINE WITH (STRAIGHTS, ARCS, OtherModel.Klothoide) VERTEX CoordD;",
            Description: """
                3.8.12.2-31 LineFormType qualified with a model name (Model-Name '.' LineFormType-Name): the custom
                form is a Reference<LineFormTypeDef> preserving the qualification. The comparison agrees on rejection
                for different reasons — ili2c fails to parse the qualified name (RefHB 3.8.12.2-30 allows it), while
                this compiler parses it and then reports the reference unresolvable (OtherModel is not imported).
                """,
            RefHB: "3.8.12.2-30",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    LineForms = { new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("STRAIGHTS") } }, new Reference<LineFormTypeDef> { Path = { new("INTERLIS"), new("ARCS") } }, new Reference<LineFormTypeDef> { Path = { new("OtherModel"), new("Klothoide") } } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 74),
                },
            }));

        yield return FullFile(new(
            "Custom line form in a WITH list resolves",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                STRUCTURE BezierSegment EXTENDS INTERLIS.LineSegment =
                    ControlX: 0.000 .. 100.000;
                END BezierSegment;
                LINE FORM Bezier : BezierSegment;
                TOPIC Topic =
                    CLASS ClassA =
                        Geom : POLYLINE WITH (STRAIGHTS, Bezier) VERTEX CoordD;
                    END ClassA;
                END Topic;
            END Model.
            """,
            Description: """
                A custom line form used in a WITH (...) list is a registered reference resolving to the model's
                LINE FORM definition, so the model compiles clean. The line structure extends INTERLIS.LineSegment
                as every line structure must (RefHB 3.8.12.3-4).
                """,
            RefHB: "3.8.12.2-30",
            Ili2cDivergenceReason: "ili2c requires the model to be CONTRACTED to define line forms — a leftover INTERLIS 2.3 contract rule; RefHB 3.5.1-12 states CONTRACTED has no function anymore and is kept only for compatibility, so we accept line form definitions in any model.",
            AssertOutput: false));

        yield return FullFile(new(
            "Unknown custom line form is reported",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS ClassA =
                        Geom : POLYLINE WITH (STRAIGHTS, Klothoide) VERTEX CoordD;
                    END ClassA;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Could not resolve 'reference 'Klothoide' from Model.Topic.ClassA' at 7:45-7:54"],
            RefHB: "3.8.12.2-30",
            AssertOutput: false));

        yield return Rule(new(
            "Polyline without line form",
            "Attr : POLYLINE VERTEX CoordD;",
            Description: "3.8.12.2-32 ControlPoints: VERTEX only, no LineForm (line form may be omitted).",
            RefHB: "3.8.12.2-31",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new PolyLineType
                {
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 29),
                },
            }));

        yield return Rule(new(
            "Surface without overlaps without explicit tolerance",
            "Attr : SURFACE VERTEX CoordD WITHOUT OVERLAPS;",
            Description: "3.8.12.2-33 IntersectionDef: WITHOUT OVERLAPS without an explicit decimal tolerance.",
            RefHB: "3.8.12.2-32",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new SurfaceType
                {
                    VertexType = new Reference<DomainDef> { Path = { new("CoordD") } },
                    WithoutOverlaps = new WithoutOverlapsDef.Implicit(),
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 45),
                },
            }));

        yield return FullFile(new(
            "Polyline domain on generic coordinate",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Coord2 (GENERIC) = COORD NUMERIC, NUMERIC;
                    Line = POLYLINE WITH (STRAIGHTS, ARCS) VERTEX Coord2;
            END Model.
            """,
            Description: "3.8.12.2-36 Worked example: line type built on a generic coordinate domain.",
            RefHB: "3.8.12.2-34",
            AssertOutput: false));

        yield return FullFile(new(
            "Surface extended to area",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS Base =
                        Geom : SURFACE WITH (STRAIGHTS) VERTEX CoordD;
                    END Base;
                    CLASS Sub EXTENDS Base =
                        Geom (EXTENDED) : AREA WITH (STRAIGHTS) VERTEX CoordD;
                    END Sub;
                END Topic;
            END Model.
            """,
            Description: "Extensibility: SURFACE may be redefined as AREA in an extension.",
            RefHB: "3.8.13.4-3",
            Ili2cDivergenceReason: "ili2c requires an explicit WITHOUT OVERLAPS on surface and area types and rejects the omission as incomplete; RefHB 3.8.12.2-22 explicitly permits omitting it (overlap-freedom is mandatory for surfaces and area partitions anyway, and the tolerance is implicit or inherited per 3.8.13.1-13), so we accept.",
            AssertOutput: false));
    }

    // Define the vertex coordinate domain so both compilers resolve it (ili2c requires VERTEX).
    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            DOMAIN
                CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
            TOPIC Topic =
                CLASS ClassName =
                    {fragment}
                END ClassName;
            END Topic;
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(LineTypeTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadAttributeDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitAttributeDef(p.attributeDef()));
    }
}
