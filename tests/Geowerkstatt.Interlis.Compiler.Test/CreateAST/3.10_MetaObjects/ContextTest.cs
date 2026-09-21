using Geowerkstatt.Interlis.Compiler.AST;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class ContextTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Context coordinate concretization",
            "CONTEXT Ctx = GCoord = CCoord;",
            Description: """
                The rule-level case references undefined domains (only meaningful with surrounding generic/concrete
                coordinate domains): its AST is verified by ReadContextDef, but the comparison diverges (ili2c rejects
                the undefined domains) and is left red as a known compiler-gap signal. The self-contained full-file case
                exercises the comparison with both compilers accepting.
                """,
            RefHB: "3.8.8-27",
            Expected: new List<ContextDef>
            {
                new ContextDef
                {
                    Name = "Ctx",
                    NameLocations = { new RangePosition(0, 8, 0, 11) },
                    Mappings =
                    {
                        new ContextMapping
                        {
                            GenericCoord = new Reference<DomainDef> { Path = { "GCoord" }, SourceRange = new RangePosition(0, 14, 0, 20) },
                            Concrete =
                            {
                                new Reference<DomainDef> { Path = { "CCoord" }, SourceRange = new RangePosition(0, 23, 0, 29) },
                            },
                        },
                    },
                },
            }));

        yield return FullFile(new(
            "Context with generic and concrete coordinates",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    GCoord (GENERIC) = COORD NUMERIC, NUMERIC;
                    CCoord EXTENDS GCoord = COORD 0.000 .. 9.000, 0.000 .. 9.000;

                CONTEXT Ctx = GCoord = CCoord;
            END Model.
            """,
            RefHB: "3.8.8-27",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference system axis Unit parameter",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC CoordSystems =
                    STRUCTURE Axis =
                        PARAMETER
                            Unit : NUMERIC [m];
                    END Axis;
                END CoordSystems;
            END RefModel.
            """,
            Description: """
                RefHB 3.10.2.1: for reference/coordinate systems (and their axes) only the predefined
                parameter Unit (NUMERIC) is allowed, modelled as an AXIS-like structure parameter.
                """,
            RefHB: "3.10.2.1-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Signature parameter METAOBJECT",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Sign =
                    PARAMETER
                        Sign : METAOBJECT;
                    END Sign;
                END Topic;
            END Model.
            """,
            Description: """
                ParameterDef: signature parameter referencing the signature class itself (METAOBJECT). A bare METAOBJECT
                parameter is the reference to the signature class the parameter is defined in (RefHB 3.10.2.2-1),
                modelled as IsMetaObject with no explicit target — like the predefined SIGN class's own Sign parameter.
                """,
            RefHB: "3.10.2.2-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Signature parameter METAOBJECT OF",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS OtherSign =
                    END OtherSign;
                    CLASS Sign =
                    PARAMETER
                        Ref : METAOBJECT OF OtherSign;
                    END Sign;
                END Topic;
            END Model.
            """,
            Description: """
                ParameterDef: signature parameter referencing another meta-object (METAOBJECT OF MetaObject-ClassRef).
                """,
            RefHB: "3.10.2.2-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Signature parameter defined like an attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Sign =
                    PARAMETER
                        Rotation : 0.000 .. 359.999;
                    END Sign;
                END Topic;
            END Model.
            """,
            Description: """
                Besides the special METAOBJECT parameters, signature parameters may be defined like attributes (here a
                numeric attribute type).
                """,
            RefHB: "3.10.2.2-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Refsystem model metaobject base class",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Systems (ABSTRACT) =
                    CLASS MetaObject (ABSTRACT) =
                        Name : MANDATORY NAME;
                        UNIQUE Name;
                    END MetaObject;
                END Systems;
            END RefModel.
            """,
            Description: """
                RefHB 3.10.3-2/-3: the predefined METAOBJECT base class shape — an ABSTRACT class with a
                MANDATORY NAME attribute and a UNIQUE Name constraint (the abstract class needs an abstract
                topic). Models the canonical INTERLIS.METAOBJECT definition self-contained.
                """,
            RefHB: "3.10.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Refsystem model axis structure",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC CoordSystems =
                    STRUCTURE Axis =
                        PARAMETER
                            Unit : NUMERIC [m];
                    END Axis;
                END CoordSystems;
            END RefModel.
            """,
            Description: "RefHB 3.10.3-4/-5: AXIS structure with a Unit parameter inside a REFSYSTEM MODEL.",
            RefHB: "3.10.3-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Refsystem model extends predefined classes",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC CoordSystems =
                    STRUCTURE Axis EXTENDS INTERLIS.AXIS =
                        PARAMETER
                            Unit (EXTENDED) : NUMERIC [m];
                    END Axis;
                    CLASS Plane EXTENDS INTERLIS.COORDSYSTEM =
                        ATTRIBUTE
                            Axis (EXTENDED) : LIST {2} OF Axis;
                    END Plane;
                END CoordSystems;
            END RefModel.
            """,
            Description: """
                RefHB 3.10.3-6/-7/-9 canonical structure: refsystem classes extend the predefined INTERLIS
                refsystem classes (AXIS, COORDSYSTEM), which are provided by the internal INTERLIS model, so the
                extends references resolve.
                """,
            RefHB: "3.10.3-6",
            Ili2cDivergenceReason: "ili2c demands that a unit concretizing the predefined AXIS/SCALSYSTEM parameter (base unit INTERLIS.ANYUNIT) explicitly EXTENDS ANYUNIT and rejects a unit with its own abstract root; RefHB 3.9.1-4 states every unit inherits ANYUNIT implicitly without declaring it, so we accept.",
            AssertOutput: false));

        yield return FullFile(new(
            "Refsystem model coordinate system with axis list",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC CoordSystems =
                    STRUCTURE Axis =
                        PARAMETER
                            Unit : NUMERIC [m];
                    END Axis;
                    CLASS CoordSystem =
                        ATTRIBUTE
                            Axes : LIST {1..3} OF Axis;
                    END CoordSystem;
                END CoordSystems;
            END RefModel.
            """,
            Description: """
                RefHB 3.10.3-7/-8: a coordinate system holds a LIST {1..3} OF AXIS (here self-contained,
                not extending the predefined COORDSYSTEM).
                """,
            RefHB: "3.10.3-7",
            AssertOutput: false));

        yield return FullFile(new(
            "Refsystem model scalar system extends predefined",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC ScalSystems =
                    CLASS Temperature EXTENDS INTERLIS.SCALSYSTEM =
                        PARAMETER
                            Unit (EXTENDED) : NUMERIC [m];
                    END Temperature;
                END ScalSystems;
            END RefModel.
            """,
            Description: """
                A scalar system extends the predefined INTERLIS.SCALSYSTEM, which is provided by the internal INTERLIS
                model, so the extends reference resolves.
                """,
            RefHB: "3.10.3-9",
            Ili2cDivergenceReason: "ili2c demands that a unit concretizing the predefined AXIS/SCALSYSTEM parameter (base unit INTERLIS.ANYUNIT) explicitly EXTENDS ANYUNIT and rejects a unit with its own abstract root; RefHB 3.9.1-4 states every unit inherits ANYUNIT implicitly without declaring it, so we accept.",
            AssertOutput: false));

        yield return FullFile(new(
            "Refsystem model scalar system class",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Temperature (ABSTRACT);
                    Kelvin EXTENDS Temperature;
                TOPIC ScalSystems =
                    CLASS ScalSystem =
                        PARAMETER
                            Unit : NUMERIC [Kelvin];
                    END ScalSystem;
                END ScalSystems;
            END RefModel.
            """,
            Description: """
                RefHB 3.10.3-9/-10: a scalar system class — the SCALSYSTEM shape — holds a Unit PARAMETER
                (NUMERIC bound to a unit) and ends with END (here self-contained, not extending the
                predefined INTERLIS.SCALSYSTEM, so both compilers accept it).
                """,
            RefHB: "3.10.3-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Basket-declared meta object reference is accepted",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC HeightSystems =
                    CLASS HeightSystem EXTENDS INTERLIS.SCALSYSTEM =
                    END HeightSystem;
                END HeightSystems;
            END RefModel.
            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
                IMPORTS RefModel;
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                REFSYSTEM BASKET Heights ~ RefModel.HeightSystems
                    OBJECTS OF HeightSystem : LN02;
                DOMAIN
                    Height = 0.000 .. 9000.000 [m] {Heights.LN02};
            END Model.
            """,
            Description: """
                Meta objects usable in models must be declared by a BASKET (basket name, presumed topic, expected object
                names per class); a basket-qualified {basket.metaObject} reference then resolves the basket and requires
                the meta-object name among its declared objects (RefHB 3.10.1-3). Both compilers accept the declared
                form and reject the undeclared name.
                """,
            RefHB: "3.10.1-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Meta object declared by an inherited basket is accepted",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                TOPIC HeightSystems =
                    CLASS HeightSystem EXTENDS INTERLIS.SCALSYSTEM =
                    END HeightSystem;
                END HeightSystems;
            END RefModel.
            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
                IMPORTS RefModel;
                REFSYSTEM BASKET Base ~ RefModel.HeightSystems
                    OBJECTS OF HeightSystem : LN02;
                REFSYSTEM BASKET Refined EXTENDS Base ~ RefModel.HeightSystems;
                DOMAIN
                    Height = 0.000 .. 9000.000 {Refined.LN02};
            END Model.
            """,
            Description: """
                A meta object referenced under an extending basket name may be declared by an inherited basket (the
                runtime searches the concrete container first, then the inherited ones).
                """,
            RefHB: "3.10.1-3",
            AssertOutput: false));

        yield return FullFile(new(
            "Undeclared meta object in a basket reference is reported",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                TOPIC HeightSystems =
                    CLASS HeightSystem EXTENDS INTERLIS.SCALSYSTEM =
                    END HeightSystem;
                END HeightSystems;
            END RefModel.
            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
                IMPORTS RefModel;
                REFSYSTEM BASKET Heights ~ RefModel.HeightSystems
                    OBJECTS OF HeightSystem : LN02;
                DOMAIN
                    Height = 0.000 .. 9000.000 {Heights.LFP1};
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Height' at 13:8-13:50: the basket 'Heights' does not declare the meta object 'LFP1'."],
            RefHB: "3.10.1-3",
            AssertOutput: false));

        yield return FullFile(new(
            "Basket-declared meta object reference with axis is accepted",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC CoordSystems =
                    CLASS CoordSystem EXTENDS INTERLIS.COORDSYSTEM =
                    END CoordSystem;
                END CoordSystems;
            END RefModel.
            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
                IMPORTS RefModel;
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                REFSYSTEM BASKET Coords ~ RefModel.CoordSystems
                    OBJECTS OF CoordSystem : LV95;
                DOMAIN
                    Easting = 0.000 .. 1000.000 [m] {Coords.LV95[1]};
            END Model.
            """,
            Description: "The meta-object form allows an axis index ({basket.metaObject[n]}).",
            RefHB: "3.8.5-19",
            Ili2cDivergenceReason: "ili2c can not parse the axis index of the meta-object reference-system form (expecting '}', found '[') although RefHB 3.8.5-19 allows it in both RefSys forms, so it rejects what we accept.",
            AssertOutput: false));

        yield return FullFile(new(
            "Numeric value references coordinate system",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC CoordSystems =
                    STRUCTURE Axis =
                        PARAMETER
                            Unit : NUMERIC [m];
                    END Axis;
                    CLASS HeightSystem =
                        PARAMETER
                            Unit : NUMERIC [m];
                    END HeightSystem;
                END CoordSystems;
            END RefModel.

            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
                IMPORTS RefModel;
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                DOMAIN
                    Height = 0.000 .. 9000.000 [m] <RefModel.CoordSystems.HeightSystem>;
            END Model.
            """,
            Description: """
                RefHB 3.8.5-19: the '<...>' form takes a Coord-DomainRef — the reference is typed to DomainDef, so a
                CLASS target stays unresolved (ili2c rejects it too: "There is neither a domain ... nor ..."); the valid
                meta-object form is pinned by the basket-declared meta object reference cases.
                """,
            ExpectedLog: ["Could not resolve 'reference 'RefModel.CoordSystems.HeightSystem' from Model' at 24:40-24:74"],
            RefHB: "3.10.3-11",
            AssertOutput: false));

        yield return FullFile(new(
            "Numeric value references coordinate system axis",
            """
            INTERLIS 2.4;
            REFSYSTEM MODEL RefModel (en) AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                TOPIC CoordSystems =
                    STRUCTURE Axis =
                        PARAMETER
                            Unit : NUMERIC [m];
                    END Axis;
                    CLASS CHCoordSystem =
                        ATTRIBUTE
                            Axes : LIST {2} OF Axis;
                    END CHCoordSystem;
                END CoordSystems;
            END RefModel.

            MODEL Model (en) AT "http://example.com" VERSION "1.0.0" =
                IMPORTS RefModel;
                UNIT
                    Meter (ABSTRACT);
                    m EXTENDS Meter;
                DOMAIN
                    Easting = 0.000 .. 1000.000 [m] {RefModel.CoordSystems.CHCoordSystem [1]};
            END Model.
            """,
            Description: """
                The refSys form (RefHB 3.8.5): a numeric value referencing a single axis of a coordinate system via the
                '{System [axis]}' frame form.
                """,
            ExpectedLog: ["Could not resolve 'reference 'RefModel.CoordSystems' from Model' at 24:41-24:62"],
            RefHB: "3.10.3-11",
            AssertOutput: false));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            {fragment}
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(ContextTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadContextDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitContextDef(p.contextDef()));
    }
}
