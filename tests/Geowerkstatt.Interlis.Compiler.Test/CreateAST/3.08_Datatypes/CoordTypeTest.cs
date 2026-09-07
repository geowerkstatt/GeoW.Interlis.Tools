using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class CoordTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Coord attribute",
            "Attr : COORD 0..100, 0..100;",
            RefHB: "3.8.8-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 100, Precision = 0, SourceRange = new RangePosition(0, 13, 0, 19), },
                        new DecimalType { Min = 0, Max = 100, Precision = 0, SourceRange = new RangePosition(0, 21, 0, 27), },
                    },
                    SourceRange = new RangePosition(0, 7, 0, 27),
                },
            }));

        yield return Rule(new(
            "Coord with rotation and reference system",
            "Attr : COORD 0 .. 9, 0 .. 9, ROTATION 2 -> 1 REFSYS \"EPSG\";",
            RefHB: "3.8.8-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 13, 0, 19) },
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 21, 0, 27) },
                    },
                    Rotation = new RotationDef { NullAxis = 2, PiHalfAxis = 1 },
                    RefSysCode = "EPSG",
                    SourceRange = new RangePosition(0, 7, 0, 58),
                },
            }));

        yield return Rule(new(
            "One-dimensional coord",
            "Attr : COORD 0.000 .. 100.000;",
            RefHB: "3.8.8-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 100, Precision = -3, SourceRange = new RangePosition(0, 13, 0, 29) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 29),
                },
            }));

        yield return Rule(new(
            "Three-dimensional coord",
            "Attr : COORD 0 .. 9, 0 .. 9, 0 .. 9;",
            RefHB: "3.8.8-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 13, 0, 19) },
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 21, 0, 27) },
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 29, 0, 35) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 35),
                },
            }));

        yield return Rule(new(
            "CHKoord example with units, refsys axes, rotation and EPSG refsys",
            "CHKoord : COORD 480000.00 .. 850000.00 [m] {CHLV03[1]}, 60000.00 .. 320000.00 [m] {CHLV03[2]}, ROTATION 2 -> 1 REFSYS \"EPSG:21781\";",
            RefHB: "3.8.8-4",
            Expected: new AttributeDef
            {
                Name = "CHKoord",
                NameLocations = { new RangePosition(0, 0, 0, 7) },
                TypeDef = new CoordType
                {
                    Axis =
                    {
                        new DecimalType
                        {
                            Min = 480000,
                            Max = 850000,
                            Precision = -2,
                            Unit = new Reference<UnitDef> { Path = { "m" }, SourceRange = new RangePosition(0, 40, 0, 41) },
                            RefSystem = new RefSys
                            {
                                Value = new RefSys.MetaObjectRef
                                {
                                    MetaObject = new Reference<MetaObjectDeclaration> { Path = { "CHLV03" }, SourceRange = new RangePosition(0, 44, 0, 50) },
                                },
                                Axis = 1,
                            },
                            SourceRange = new RangePosition(0, 16, 0, 54),
                        },
                        new DecimalType
                        {
                            Min = 60000,
                            Max = 320000,
                            Precision = -2,
                            Unit = new Reference<UnitDef> { Path = { "m" }, SourceRange = new RangePosition(0, 79, 0, 80) },
                            RefSystem = new RefSys
                            {
                                Value = new RefSys.MetaObjectRef
                                {
                                    MetaObject = new Reference<MetaObjectDeclaration> { Path = { "CHLV03" }, SourceRange = new RangePosition(0, 83, 0, 89) },
                                },
                                Axis = 2,
                            },
                            SourceRange = new RangePosition(0, 56, 0, 93),
                        },
                    },
                    Rotation = new RotationDef { NullAxis = 2, PiHalfAxis = 1 },
                    RefSysCode = "EPSG:21781",
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 10, 0, 130),
                },
            }));

        yield return Rule(new(
            "WGS84 example with degree units, circular axis and refsys axes",
            "WGS84Koord : COORD -90.00000 .. 90.00000 [Units.Angle_Degree] {WGS84[1]}, 0.00000 .. 359.99999 CIRCULAR [Units.Angle_Degree] {WGS84[2]}, -1000.00 .. 9000.00 [m] {WGS84Alt[1]};",
            RefHB: "3.8.8-7",
            Expected: new AttributeDef
            {
                Name = "WGS84Koord",
                NameLocations = { new RangePosition(0, 0, 0, 10) },
                TypeDef = new CoordType
                {
                    Axis =
                    {
                        new DecimalType
                        {
                            Min = -90,
                            Max = 90,
                            Precision = -5,
                            Unit = new Reference<UnitDef> { Path = { "Units", "Angle_Degree" }, SourceRange = new RangePosition(0, 42, 0, 60) },
                            RefSystem = new RefSys
                            {
                                Value = new RefSys.MetaObjectRef
                                {
                                    MetaObject = new Reference<MetaObjectDeclaration> { Path = { "WGS84" }, SourceRange = new RangePosition(0, 63, 0, 68) },
                                },
                                Axis = 1,
                            },
                            SourceRange = new RangePosition(0, 19, 0, 72),
                        },
                        new DecimalType
                        {
                            Min = 0,
                            Max = 359.99999,
                            Precision = -5,
                            Circular = true,
                            Unit = new Reference<UnitDef> { Path = { "Units", "Angle_Degree" }, SourceRange = new RangePosition(0, 105, 0, 123) },
                            RefSystem = new RefSys
                            {
                                Value = new RefSys.MetaObjectRef
                                {
                                    MetaObject = new Reference<MetaObjectDeclaration> { Path = { "WGS84" }, SourceRange = new RangePosition(0, 126, 0, 131) },
                                },
                                Axis = 2,
                            },
                            SourceRange = new RangePosition(0, 74, 0, 135),
                        },
                        new DecimalType
                        {
                            Min = -1000,
                            Max = 9000,
                            Precision = -2,
                            Unit = new Reference<UnitDef> { Path = { "m" }, SourceRange = new RangePosition(0, 158, 0, 159) },
                            RefSystem = new RefSys
                            {
                                Value = new RefSys.MetaObjectRef
                                {
                                    MetaObject = new Reference<MetaObjectDeclaration> { Path = { "WGS84Alt" }, SourceRange = new RangePosition(0, 162, 0, 170) },
                                },
                                Axis = 1,
                            },
                            SourceRange = new RangePosition(0, 137, 0, 174),
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 13, 0, 174),
                },
            }));

        yield return Rule(new(
            "Coord with circular axis",
            "Attr : COORD 0.00000 .. 359.99999 CIRCULAR;",
            RefHB: "3.8.8-8",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 359.99999, Precision = -5, Circular = true, SourceRange = new RangePosition(0, 13, 0, 42) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 42),
                },
            }));

        yield return Rule(new(
            "Multicoord with three axes, rotation and refsys (full CoordinateType production)",
            "Attr : MULTICOORD 0 .. 9, 0 .. 9, 0 .. 9, ROTATION 2 -> 1 REFSYS \"EPSG:21781\";",
            RefHB: "3.8.8-12",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    IsMultiGeometry = true,
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 18, 0, 24) },
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 26, 0, 32) },
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 34, 0, 40) },
                    },
                    Rotation = new RotationDef { NullAxis = 2, PiHalfAxis = 1 },
                    RefSysCode = "EPSG:21781",
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 77),
                },
            }));

        yield return Rule(new(
            "Coord rotation without refsys",
            "Attr : COORD 0 .. 9, 0 .. 9, ROTATION 2 -> 1;",
            RefHB: "3.8.8-13",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 13, 0, 19) },
                        new DecimalType { Min = 0, Max = 9, Precision = 0, SourceRange = new RangePosition(0, 21, 0, 27) },
                    },
                    Rotation = new RotationDef { NullAxis = 2, PiHalfAxis = 1 },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 44),
                },
            }));

        yield return Rule(new(
            "Coord with EPSG code reference system",
            "Attr : COORD 0.00 .. 100.00, 0.00 .. 100.00 REFSYS \"EPSG:21781\";",
            RefHB: "3.8.8-14",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 100, Precision = -2, SourceRange = new RangePosition(0, 13, 0, 27) },
                        new DecimalType { Min = 0, Max = 100, Precision = -2, SourceRange = new RangePosition(0, 29, 0, 43) },
                    },
                    RefSysCode = "EPSG:21781",
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 63),
                },
            }));

        yield return Rule(new(
            "Multicoord attribute",
            "Attr : MULTICOORD 0.000 .. 100.000, 0.000 .. 100.000;",
            RefHB: "3.8.8-15",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    IsMultiGeometry = true,
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 100, Precision = -3, SourceRange = new RangePosition(0, 18, 0, 34) },
                        new DecimalType { Min = 0, Max = 100, Precision = -3, SourceRange = new RangePosition(0, 36, 0, 52) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 52),
                },
            }));

        yield return FullFile(new(
            "Abstract coord domain with open dimensions",
            """
            INTERLIS 2.4;
            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                AbstractCoord (ABSTRACT) = COORD NUMERIC;
            END Model.
            """,
            RefHB: "3.8.8-17",
            AssertOutput: false));

        yield return FullFile(new(
            "Generic coord domain with deferred generics and class usage",
            """
            INTERLIS 2.4;
            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
                DEFERRED GENERICS Coord2;
                DOMAIN
                  Coord2 (GENERIC) = COORD NUMERIC, NUMERIC;
                CLASS Punkt =
                  Pos: Coord2;
                END Punkt;
              END Topic;
            END Model.
            """,
            Description: """
                The compiler does not yet resolve generic domain references (DEFERRED GENERICS), so the generic
                'Coord2' used by the class attribute is currently reported as unresolved.
                """,
            ExpectedLog: ["Could not resolve 'reference 'Coord2' from Model'"],
            RefHB: "3.8.8-20",
            AssertOutput: false));

        yield return FullFile(new(
            "Generic coord domain without deferred generics is rejected",
            """
            INTERLIS 2.4;
            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
                DOMAIN
                  Coord2 (GENERIC) = COORD NUMERIC, NUMERIC;
                CLASS Punkt =
                  Pos: Coord2;
                END Punkt;
              END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic': must declare DEFERRED GENERICS for the generic domain 'Coord2'."],
            RefHB: "3.8.8-23",
            AssertOutput: false));

        yield return FullFile(new(
            "Context definition resolving a generic coord domain",
            """
            INTERLIS 2.4;
            MODEL GeometryCHLV03 (en) AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Coord2 = COORD 0.000 .. 100.000, 0.000 .. 100.000;
            END GeometryCHLV03.

            MODEL GeometryCHLV95 (en) AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Coord2 = COORD 0.000 .. 200.000, 0.000 .. 200.000;
            END GeometryCHLV95.

            MODEL MyModel (en) AT "http://example.com" VERSION "1.0.0" =
              IMPORTS GeometryCHLV03;
              IMPORTS GeometryCHLV95;
              CONTEXT default =
                MyModel.Coord2 = GeometryCHLV03.Coord2 OR GeometryCHLV95.Coord2;
              TOPIC Topic =
                DEFERRED GENERICS Coord2;
                DOMAIN
                  Coord2 (GENERIC) = COORD NUMERIC, NUMERIC;
              END Topic;
            END MyModel.
            """,
            Description: """
                The compiler does not yet resolve the CONTEXT definition nor the generic domain it binds, so both
                the context target 'MyModel.Coord2' and the generic 'Coord2' are currently reported as unresolved.
                """,
            ExpectedLog:
            [
                "Could not resolve 'reference 'MyModel.Coord2' from MyModel'",
                "Could not resolve 'reference 'Coord2' from MyModel'",
            ],
            RefHB: "3.8.8-28",
            AssertOutput: false));

        yield return FullFile(new(
            "Context with concrete coord domain via EXTENDS specialization",
            """
            INTERLIS 2.4;
            MODEL GeometryCHLV03 (en) AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Coord2 = COORD 0.000 .. 100.000, 0.000 .. 100.000;
            END GeometryCHLV03.

            MODEL MyModel (en) AT "http://example.com" VERSION "1.0.0" =
              IMPORTS GeometryCHLV03;
              DOMAIN
                Coord2Special EXTENDS GeometryCHLV03.Coord2 = COORD 0.000 .. 50.000, 0.000 .. 50.000;
              CONTEXT default =
                MyModel.Coord2 = Coord2Special;
              TOPIC Topic =
                DEFERRED GENERICS Coord2;
                DOMAIN
                  Coord2 (GENERIC) = COORD NUMERIC, NUMERIC;
              END Topic;
            END MyModel.
            """,
            Description: """
                The same current gap as in 'Context definition resolving a generic coord domain': the CONTEXT target
                'MyModel.Coord2' and the generic 'Coord2' are not yet resolved, so both are reported as unresolved.
                """,
            ExpectedLog:
            [
                "Could not resolve 'reference 'MyModel.Coord2' from MyModel'",
                "Could not resolve 'reference 'Coord2' from MyModel'",
            ],
            RefHB: "3.8.8-30",
            AssertOutput: false));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
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
        => ComparisonRows(nameof(CoordTypeTest), GetCases(), Wrap);

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
