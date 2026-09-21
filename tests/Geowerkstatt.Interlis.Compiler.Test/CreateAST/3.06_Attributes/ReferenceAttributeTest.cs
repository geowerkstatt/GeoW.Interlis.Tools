using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class ReferenceAttributeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Reference attribute",
            "attr: REFERENCE TO Target;",
            RefHB: "3.6.3-1",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ReferenceType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Target" }, SourceRange = new RangePosition(0, 19, 0, 25) },
                    },                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 25),
                },
            }));

        yield return Rule(new(
            "Reference attribute with RESTRICTION",
            "attr: REFERENCE TO Base RESTRICTION (Sub);",
            RefHB: "3.6.3-2",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ReferenceType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Base" }, SourceRange = new RangePosition(0, 19, 0, 23) },
                        Restrictions = { new Reference<IInterlisDefinition> { Path = { "Sub" }, SourceRange = new RangePosition(0, 37, 0, 40) } },
                    },                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 41),
                },
            }));

        yield return Rule(new(
            "Reference attribute with multiple RESTRICTION classes",
            "attr: REFERENCE TO Base RESTRICTION (SubA; SubB);",
            RefHB: "3.6.3-2",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ReferenceType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Base" }, SourceRange = new RangePosition(0, 19, 0, 23) },
                        Restrictions =
                        {
                            new Reference<IInterlisDefinition> { Path = { "SubA" }, SourceRange = new RangePosition(0, 37, 0, 41) },
                            new Reference<IInterlisDefinition> { Path = { "SubB" }, SourceRange = new RangePosition(0, 43, 0, 47) },
                        },
                    },                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 48),
                },
            }));

        yield return Rule(new(
            "External reference attribute",
            "attr: REFERENCE TO (EXTERNAL) Target;",
            RefHB: "3.6.3-2",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ReferenceType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Target" }, SourceRange = new RangePosition(0, 30, 0, 36) },
                    },
                    Properties = { Property.External },                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 36),
                },
            }));

        yield return Rule(new(
            "External reference attribute with RESTRICTION",
            "attr: REFERENCE TO (EXTERNAL) Base RESTRICTION (Sub);",
            RefHB: "3.6.3-2",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ReferenceType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Base" }, SourceRange = new RangePosition(0, 30, 0, 34) },
                        Restrictions = { new Reference<IInterlisDefinition> { Path = { "Sub" }, SourceRange = new RangePosition(0, 48, 0, 51) } },
                    },
                    Properties = { Property.External },                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 52),
                },
            }));

        yield return Rule(new(
            "Reference attribute to ANYCLASS",
            "attr: REFERENCE TO ANYCLASS;",
            RefHB: "3.6.3-2",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ReferenceType
                {
                    Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Class },                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 27),
                },
            }));

        yield return Rule(new(
            "Reference attribute to ANYCLASS with RESTRICTION",
            "attr: REFERENCE TO ANYCLASS RESTRICTION (Base);",
            RefHB: "3.6.3-2",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ReferenceType
                {
                    Target = new RestrictedRef
                    {
                        Value = RestrictedRef.AnyKind.Class,
                        Restrictions = { new Reference<IInterlisDefinition> { Path = { "Base" }, SourceRange = new RangePosition(0, 41, 0, 45) } },
                    },                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 46),
                },
            }));

        // Cases that need a target class (concrete/abstract/struct/association), RESTRICTION subclassing,
        // EXTERNAL/DEPENDS ON across topics or extension semantics cannot be parsed with the attributeDef
        // rule alone, they are only used for the compiler comparison.
        yield return FullFile(new(
            "Reference attribute to struct",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    STRUCTURE Struct =
                    END Struct;
                    CLASS Class =
                        attr: REFERENCE TO Struct;
                    END Class;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.Class -> attr' at 7:12-7:38: a reference attribute may only reference a class."],
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference attribute to concrete class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Target =
                    END Target;
                    CLASS Source =
                        attr: REFERENCE TO Target;
                    END Source;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference attribute to association class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                    END ClassA;
                    CLASS ClassB =
                    END ClassB;
                    ASSOCIATION Assoc =
                        RoleA -- ClassA;
                        RoleB -- ClassB;
                    END Assoc;
                    CLASS Source =
                        attr: REFERENCE TO Assoc;
                    END Source;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.Source -> attr' at 13:12-13:37: a reference attribute may only reference a class."],
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference with RESTRICTION to valid subclass",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Base (ABSTRACT) =
                    END Base;
                    CLASS Sub EXTENDS Base =
                    END Sub;
                    CLASS Source =
                        attr: REFERENCE TO Base RESTRICTION (Sub);
                    END Source;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference with RESTRICTION to non-subclass",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Base =
                    END Base;
                    CLASS Other =
                    END Other;
                    CLASS Source =
                        attr: REFERENCE TO Base RESTRICTION (Other);
                    END Source;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.Source -> attr' at 9:12-9:56: RESTRICTION 'Other' must be an extension of 'Base'."],
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference with RESTRICTION to base class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Base =
                    END Base;
                    CLASS Source =
                        attr: REFERENCE TO Base RESTRICTION (Base);
                    END Source;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference with multiple RESTRICTION classes",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Base (ABSTRACT) =
                    END Base;
                    CLASS SubA EXTENDS Base =
                    END SubA;
                    CLASS SubB EXTENDS Base =
                    END SubB;
                    CLASS Source =
                        attr: REFERENCE TO Base RESTRICTION (SubA; SubB);
                    END Source;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Extension narrows RESTRICTION TO",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Base (ABSTRACT) =
                    END Base;
                    CLASS SubA EXTENDS Base =
                    END SubA;
                    CLASS SubB EXTENDS Base =
                    END SubB;
                    CLASS Source (ABSTRACT) =
                        attr: REFERENCE TO Base RESTRICTION (SubA; SubB);
                    END Source;
                    CLASS SubSource EXTENDS Source =
                        attr (EXTENDED): REFERENCE TO Base RESTRICTION (SubA);
                    END SubSource;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Extension RESTRICTION TO class not subclass of previous restriction",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Base (ABSTRACT) =
                    END Base;
                    CLASS SubA EXTENDS Base =
                    END SubA;
                    CLASS SubB EXTENDS Base =
                    END SubB;
                    CLASS Source (ABSTRACT) =
                        attr: REFERENCE TO Base RESTRICTION (SubA);
                    END Source;
                    CLASS SubSource EXTENDS Source =
                        attr (EXTENDED): REFERENCE TO Base RESTRICTION (SubB);
                    END SubSource;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference with EXTERNAL to class in dependent topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    CLASS Target =
                    END Target;
                END TopicA;
                TOPIC TopicB =
                    DEPENDS ON TopicA;
                    CLASS Source =
                        attr: REFERENCE TO (EXTERNAL) Model.TopicA.Target;
                    END Source;
                END TopicB;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference without EXTERNAL to class in other topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    CLASS Target =
                    END Target;
                END TopicA;
                TOPIC TopicB =
                    DEPENDS ON TopicA;
                    CLASS Source =
                        attr: REFERENCE TO Model.TopicA.Target;
                    END Source;
                END TopicB;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.TopicB.Source -> attr' at 10:12-10:51: a cross-topic reference requires property EXTERNAL."],
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference to a class of the base topic needs no EXTERNAL",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Base =
                    CLASS Target =
                    END Target;
                END Base;
                TOPIC Extended EXTENDS Base =
                    CLASS Source =
                        attr: REFERENCE TO Model.Base.Target;
                    END Source;
                END Extended;
            END Model.
            """,
            Description: """
                A class declared in a BASE topic of the referencing topic is not external: the extending topic inherits
                the base topic's classes (RefHB 3.5.4-11), so no EXTERNAL is needed — real-world case:
                Nutzungsplanung_NWOW_V2.Geobasisdaten EXTENDS the base model's Planungsperimeter topic and references
                its Planungsperimeter class.
                """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference with EXTERNAL but missing DEPENDS ON",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    CLASS Target =
                    END Target;
                END TopicA;
                TOPIC TopicB =
                    CLASS Source =
                        attr: REFERENCE TO (EXTERNAL) Model.TopicA.Target;
                    END Source;
                END TopicB;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.TopicB.Source -> attr' at 9:12-9:62: the EXTERNAL reference requires a topic dependency on 'TopicA'."],
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Extension removes EXTERNAL property",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    CLASS Target =
                    END Target;
                END TopicA;
                TOPIC TopicB =
                    DEPENDS ON TopicA;
                    CLASS SubTarget EXTENDS Model.TopicA.Target =
                    END SubTarget;
                    CLASS Source (ABSTRACT) =
                        attr: REFERENCE TO (EXTERNAL) Model.TopicA.Target;
                    END Source;
                    CLASS SubSource EXTENDS Source =
                        attr (EXTENDED): REFERENCE TO Model.TopicA.Target RESTRICTION (SubTarget);
                    END SubSource;
                END TopicB;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Extension adds EXTERNAL property (invalid)",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicB =
                    CLASS Target =
                    END Target;
                    CLASS Source (ABSTRACT) =
                        attr: REFERENCE TO Target;
                    END Source;
                    CLASS SubSource EXTENDS Source =
                        attr (EXTENDED): REFERENCE TO (EXTERNAL) Target;
                    END SubSource;
                END TopicB;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Abstract reference attribute with no concrete subclass",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic (ABSTRACT) =
                    CLASS Target (ABSTRACT) =
                    END Target;
                    CLASS Source (ABSTRACT) =
                        attr (ABSTRACT): REFERENCE TO Target;
                    END Source;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference to abstract class with no concrete subclass",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicB (ABSTRACT) =
                    CLASS Target (ABSTRACT) =
                    END Target;
                    CLASS Source =
                        attr: REFERENCE TO Target;
                    END Source;
                END TopicB;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference to abstract class with concrete subclass",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Target (ABSTRACT) =
                    END Target;
                    CLASS ConcreteSub EXTENDS Target =
                    END ConcreteSub;
                    CLASS Source =
                        attr: REFERENCE TO Target;
                    END Source;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference attribute to ANYSTRUCTURE is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        attr: REFERENCE TO ANYSTRUCTURE;
                    END Class;
                END Topic;
            END Model.
            """,
            Description: """
                A reference attribute targets a class or association (RestrictedClassOrAssRef, RefHB 3.6.1-15); ANYSTRUCTURE
                (a structure placeholder, RefHB 3.6.1-17) is not a valid reference target.
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.Class -> attr' at 5:12-5:44: ANYSTRUCTURE is not allowed as a reference target."],
            RefHB: "3.6.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Restriction class with a circular base chain does not hang",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Target =
                    END Target;
                    CLASS B EXTENDS C =
                    END B;
                    CLASS C EXTENDS B =
                    END C;
                    CLASS D =
                        r : REFERENCE TO Target RESTRICTION (B);
                    END D;
                END Topic;
            END Model.
            """,
            Description: """
                The RESTRICTION check walks the restriction class's EXTENDS chain; a circular chain that never reaches
                the referenced class must terminate (it used to hang the compiler) and yields the normal restriction
                error, alongside the cycle diagnostics on the classes themselves.
                """,
            ExpectedLog:
            [
                "Type check error in 'Model.Topic.B' at 6:8-7:14: the class transitively EXTENDS itself.",
                "Type check error in 'Model.Topic.C' at 8:8-9:14: the class transitively EXTENDS itself.",
                "Type check error in 'Model.Topic.D -> r' at 11:12-11:52: RESTRICTION 'B' must be an extension of 'Target'.",
            ],
            RefHB: "3.6.3-3",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference attribute to a NO OID class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS N =
                        NO OID;
                    END N;
                    STRUCTURE S =
                        r : REFERENCE TO N;
                    END S;
                END Topic;
            END Model.
            """,
            Description: """
                NO OID declares the class's object identification unstable, and as a consequence no references —
                relationships or reference attributes — may be defined onto the class (RefHB 3.5.3-2). Only the
                explicit opt-out counts: a class without any OID definition stays referenceable.
                """,
            RefHB: "3.5.3-2",
            ExpectedLog: ["Type check error in 'Model.Topic.S -> r' at 8:12-8:31: can not reference 'N' because it has no stable object identification (NO OID)."],
            Ili2cDivergenceReason: """
                ili2c accepts references to classes declared with NO OID; RefHB 3.5.3-2 states such references can
                not be defined, so we reject.
                """,
            AssertOutput: false));


        yield return FullFile(new(
            "Reference to a class inheriting the topic's OID assignment",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    OID AS INTERLIS.STANDARDOID;
                    CLASS N =
                    END N;
                    STRUCTURE S =
                        r : REFERENCE TO N;
                    END S;
                END Topic;
            END Model.
            """,
            Description: """
                A class without its own OID definition falls back to the topic's assignment (RefHB 3.5.3-2,
                "Fehlt die Definition, gilt diejenige des Themas") — here a stable STANDARDOID, so the class is
                referenceable.
                """,
            RefHB: "3.5.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference to a class inheriting an unstable topic OID assignment",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    OID AS INTERLIS.NOOID;
                    CLASS N =
                    END N;
                    STRUCTURE S =
                        r : REFERENCE TO N;
                    END S;
                END Topic;
            END Model.
            """,
            Description: """
                The topic default the class falls back to is the unstable NOOID, so the class has no stable object
                identification and can not be referenced (RefHB 3.5.3-2). The comparison agrees for an unrelated
                reason: ili2c's predefined model does not contain the NOOID domain Annex A declares, so it rejects
                the assignment as an unknown name.
                """,
            RefHB: "3.5.3-2",
            ExpectedLog: ["Type check error in 'Model.Topic.S -> r' at 8:12-8:31: can not reference 'N' because it has no stable object identification (NO OID)."],
            AssertOutput: false));


        yield return FullFile(new(
            "Reference to a class without any OID definition",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS N =
                    END N;
                    STRUCTURE S =
                        r : REFERENCE TO N;
                    END S;
                END Topic;
            END Model.
            """,
            Description: """
                With no OID definition on the class chain or the topic, RefHB 3.5.3-2's implicit NO OID applies
                ("Fehlt die Definition auch beim Thema, gilt implizit NO OID"), which by the letter would forbid
                the reference. The reference is deliberately accepted anyway: the published ecosystem — the
                federal base models among 172 repository models at the time of writing — and the RefHB's own
                association examples define references without any OID declaration, and ili2c accepts them. Only
                a definition that resolves to NOOID is rejected.
                """,
            RefHB: "3.5.3-2",
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
        => ComparisonRows(nameof(ReferenceAttributeTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadReferenceAttribute(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitAttributeDef(p.attributeDef()));
    }
}
