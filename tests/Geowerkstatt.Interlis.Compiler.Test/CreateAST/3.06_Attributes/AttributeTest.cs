using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class AttributeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Simple attribute name and type",
            "Name : TEXT;",
            RefHB: "3.6.1-1",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 11),
                },
            }));

        yield return Rule(new(
            "Final attribute property",
            "Name (FINAL) : TEXT;",
            RefHB: "3.6.1-1",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 15, 0, 19),
                },
                Properties = { Property.Final },
            }));

        yield return FullFile(new(
            "Transient attribute with a factor is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassName =
                        Name (TRANSIENT) : TEXT*10 := "fixed";
                    END ClassName;
                END Topic;
            END Model.
            """,
            Description: """
                Transient attributes are factor-fixed attributes excluded from the transfer; without a factor assignment
                a TRANSIENT attribute has no value at all. ili2c rejects this too. The bare rule case 'Transient
                attribute without a factor is rejected' stays a parse pin whose wrapped comparison agrees on the
                rejection.
                """,
            RefHB: "3.6.1-5",
            AssertOutput: false));

        yield return FullFile(new(
            "Transient attribute without a factor is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassName =
                        Name (TRANSIENT) : TEXT*10;
                    END ClassName;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.ClassName -> Name' at 5:12-5:39: is TRANSIENT but has no factor assignment fixing its value."],
            RefHB: "3.6.1-5",
            AssertOutput: false));

        yield return Rule(new(
            "Transient attribute property",
            "Name (TRANSIENT) : TEXT;",
            RefHB: "3.6.1-5",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 19, 0, 23),
                },
                Properties = { Property.Transient },
            }));

        // Subdivision consistency, factor type compatibility and attribute-extension narrowing are
        // checked during full-file type checking, not while parsing a single attributeDef, so these
        // cases are only used for the compiler comparison.
        yield return FullFile(new(
            "Inconsistent Unit and Subdivision definition",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    Minute [min];
                    Hour [h] = 60 [min];

                STRUCTURE Time =
                    Hours: 0 .. 23 CIRCULAR [h];
                    CONTINUOUS SUBDIVISION Minutes: 0 .. 99 CIRCULAR [min];
                END Time;
            END Model.
            """,
            RefHB: "3.6.1-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Invalid subdivision of non numeric attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Time =
                    Text: TEXT;
                    SUBDIVISION Attr: 0 .. 99;
                END Time;
            END Model.
            """,
            RefHB: "3.6.1-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Invalid subdivision of fractional numeric attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Time =
                    Main: 0.00 .. 99.00;
                    SUBDIVISION Attr: 0 .. 99;
                END Time;
            END Model.
            """,
            RefHB: "3.6.1-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Invalid subdivision of negative numeric attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Time =
                    Main: -10 .. 10;
                    SUBDIVISION Attr: 0 .. 99;
                END Time;
            END Model.
            """,
            RefHB: "3.6.1-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Subdivision without precursor",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Time =
                    SUBDIVISION Attr: 0 .. 99;
                END Time;
            END Model.
            """,
            RefHB: "3.6.1-1",
            AssertOutput: false));

        yield return Rule(new(
            "Attribute definition complete",
            """
            !!@ key=value
            /** Doc-Comment */
            CONTINUOUS SUBDIVISION Attr (ABSTRACT, EXTENDED) : 0.00 .. 100.00 := PI, 0.1230e-10;
            """,
            RefHB: "3.6.1-3",
            Ech0117: "5-11",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(2, 23, 2, 27) },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                TypeDef = new DecimalType { Min = 0, Max = 100, Precision = -2, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 51, 2, 65), },
                Properties = { Property.Abstract, Property.Extended },
                Subdivision = AttributeDef.SubdivisionKind.ContinuousSubdivision,
                Values =
                {
                    new NumericConstant { Value = NumericConstant.PredefinedConstant.Pi },
                    new NumericConstant { Value = 0.1230e-10 },
                },
            }));

        yield return Rule(new(
            "Numeric constant PI with unit",
            "halfCircumference: 0.000 .. 5.000 := PI [INTERLIS.m];",
            RefHB: "3.6.1-3",
            Expected: new AttributeDef
            {
                Name = "halfCircumference",
                NameLocations = { new RangePosition(0, 0, 0, 17) },
                TypeDef = new DecimalType
                {
                    Min = 0,
                    Max = 5,
                    Precision = -3,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 19, 0, 33),
                },
                Values =
                {
                    new NumericConstant
                    {
                        Value = NumericConstant.PredefinedConstant.Pi,
                        Unit = new Reference<UnitDef> { Path = { "INTERLIS", "m" }, SourceRange = new RangePosition(0, 41, 0, 51) },
                    },
                },
            }));

        yield return FullFile(new(
            "Incompatible factor type",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        text: TEXT := 3;
                    END Class;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.1-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Extending attribute increases value range",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        attr: 0 .. 10;
                    END ClassA;
                    CLASS ClassB EXTENDS ClassA =
                        attr (EXTENDED): 0 .. 999;
                    END ClassB;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.ClassB -> attr' at 8:12-8:38: the value range must not be wider than the inherited range."],
            RefHB: "3.6.1-7",
            AssertOutput: false));

        yield return FullFile(new(
            "Extending attribute increases value range through a domain alias",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    wide = 0 .. 999;
                TOPIC Topic =
                    CLASS ClassA =
                        attr: 0 .. 10;
                    END ClassA;
                    CLASS ClassB EXTENDS ClassA =
                        attr (EXTENDED): wide;
                    END ClassB;
                END Topic;
            END Model.
            """,
            Description: """
                The narrowing check compares what the attribute types stand for: a domain alias is followed to the
                domain's effective type, so hiding the wider range behind a domain does not evade the check.
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.ClassB -> attr' at 10:12-10:34: the value range must not be wider than the inherited range."],
            RefHB: "3.6.1-7",
            AssertOutput: false));

        yield return FullFile(new(
            "Attribute with an abstract type must be declared ABSTRACT",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    open (ABSTRACT) = NUMERIC;
                CLASS ClassA (ABSTRACT) =
                    inline : NUMERIC;
                    aliased : open;
                END ClassA;
            END Model.
            """,
            Description: """
                An attribute whose type is abstract must itself be declared ABSTRACT — whether the abstract type is
                written inline or referenced through a domain.
                """,
            ExpectedLog:
            [
                "Type check error in 'Model.ClassA -> inline' at 6:8-6:25: must be declared ABSTRACT because its type is not fully defined.",
                "Type check error in 'Model.ClassA -> aliased' at 7:8-7:23: must be declared ABSTRACT because its type is not fully defined.",
            ],
            RefHB: "3.6.1-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Extended attribute with a bound-less NUMERIC",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        attr : 0 .. 10;
                    END ClassA;
                    CLASS ClassB EXTENDS ClassA =
                        attr (EXTENDED) : NUMERIC;
                    END ClassB;
                END Topic;
            END Model.
            """,
            Description: """
                A bound-less NUMERIC counts as abstract (RefHB 3.8.5-1) and abstracts the inherited concrete range
                again instead of restricting it — an extended attribute can not shed the inherited numeric bounds
                the way it inherits omitted enumeration or line-type parts (ili2c agrees on both counts).
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.ClassB -> attr' at 8:12-8:38: an abstract NUMERIC can not extend a concrete numeric range."],
            RefHB: "3.8.5-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Extending attribute narrows the cardinality",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE S =
                    v : TEXT*5;
                END S;
                TOPIC Topic =
                    CLASS ClassA =
                        opt : TEXT*10;
                        bag : BAG {1..5} OF S;
                    END ClassA;
                    CLASS ClassB EXTENDS ClassA =
                        opt (EXTENDED) : MANDATORY TEXT*10;
                        bag (EXTENDED) : BAG {2..4} OF S;
                    END ClassB;
                END Topic;
            END Model.
            """,
            Description: """
                An extension may only restrict — the cardinality must lie within the inherited one. Making an optional
                attribute MANDATORY and shrinking a collection are restrictions; the reverse directions widen and are
                rejected (ili2c agrees: "The cardinality must be more restrictive").
                """,
            RefHB: "3.6.1-6",
            AssertOutput: false));

        yield return FullFile(new(
            "Extending attribute widens the cardinality",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE S =
                    v : TEXT*5;
                END S;
                TOPIC Topic =
                    CLASS ClassA =
                        must : MANDATORY TEXT*10;
                        bag : BAG {1..5} OF S;
                    END ClassA;
                    CLASS ClassB EXTENDS ClassA =
                        must (EXTENDED) : TEXT*10;
                        bag (EXTENDED) : BAG {0..9} OF S;
                    END ClassB;
                END Topic;
            END Model.
            """,
            ExpectedLog:
            [
                "Type check error in 'Model.Topic.ClassB -> must' at 12:12-12:38: the cardinality must not be wider than the inherited cardinality.",
                "Type check error in 'Model.Topic.ClassB -> bag' at 13:12-13:45: the cardinality must not be wider than the inherited cardinality.",
            ],
            RefHB: "3.6.1-6",
            AssertOutput: false));

        yield return FullFile(new(
            "Extending attribute overrides the inherited unit",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        attr: 0 .. 10 [INTERLIS.m];
                    END ClassA;
                    CLASS ClassB EXTENDS ClassA =
                        attr (EXTENDED): 0 .. 5 [INTERLIS.s];
                    END ClassB;
                END Topic;
            END Model.
            """,
            Description: "The unit-extension rules apply to extended attributes like to domain extensions.",
            ExpectedLog: ["Type check error in 'Model.Topic.ClassB -> attr' at 8:12-8:49: the inherited concrete unit 'm' can not be overridden."],
            RefHB: "3.8.5-10",
            AssertOutput: false));

        yield return Rule(new(
            "Attribute with constant value",
            "Name : 0 .. 100 := 50;",
            RefHB: "3.6.1-8",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new DecimalType
                {
                    Min = 0,
                    Max = 100,
                    Precision = 0,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 15),
                },
                Values = {
                    new NumericConstant
                    {
                        Value = 50,
                    },
                },
            }));

        yield return FullFile(new(
            "Overriding constant-fixed attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        attr: 0 .. 10 := 5;
                    END ClassA;
                    CLASS ClassB EXTENDS ClassA =
                        attr (EXTENDED): 0 .. 8;
                    END ClassB;
                END Topic;
            END Model.
            """,
            Description: "A constant-fixed attribute is implicitly final and must not be overridden in an extension.",
            RefHB: "3.6.1-8",
            AssertOutput: false));

        yield return Rule(new(
            "Mandatory attribute with explicit type",
            "Name : MANDATORY TEXT;",
            RefHB: "3.6.1-11",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 1, Max = 1 },
                    SourceRange = new RangePosition(0, 17, 0, 21),
                },
            }));

        yield return Rule(new(
            "Bag of type attribute",
            "Name : BAG OF TEXT;",
            RefHB: "3.6.1-11",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 14, 0, 18),
                },
            }));

        yield return Rule(new(
            "List of type attribute",
            "Name : LIST OF TEXT;",
            RefHB: "3.6.1-11",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound, Ordered = true },
                    SourceRange = new RangePosition(0, 15, 0, 19),
                },
            }));

        yield return Rule(new(
            "Bag of type attribute with cardinality",
            "Name : BAG {1..5} OF TEXT;",
            RefHB: "3.6.1-11",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 1, Max = 5 },
                    SourceRange = new RangePosition(0, 21, 0, 25),
                },
            }));

        yield return Rule(new(
            "Domain reference as attribute type",
            "Name : MyDomain;",
            RefHB: "3.6.2-1",
            Expected: new AttributeDef
            {
                Name = "Name",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "MyDomain" }, SourceRange = new RangePosition(0, 7, 0, 15) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 15),
                },
            }));

        yield return FullFile(new(
            "Extending attribute with only MANDATORY",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        attr: 0 .. 10;
                    END ClassA;
                    CLASS ClassB EXTENDS ClassA =
                        attr (EXTENDED): MANDATORY;
                    END ClassB;
                END Topic;
            END Model.
            """,
            RefHB: "3.6.1-18",
            AssertOutput: false));

        yield return FullFile(new(
            "Extended attribute with a circular base chain does not hang",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS B EXTENDS C =
                    END B;
                    CLASS C EXTENDS B =
                    END C;
                    CLASS A EXTENDS B =
                        attr (EXTENDED) : TEXT*5;
                    END A;
                END Topic;
            END Model.
            """,
            Description: """
                The extended attribute's base lookup walks the EXTENDS chain; a circular chain that never yields the
                attribute must terminate (it used to hang the compiler). The classes forming the cycle are diagnosed;
                A only extends INTO the cycle — its own chain never returns to it, so it carries no cycle error.
                """,
            RefHB: "3.5.3-13",
            ExpectedLog:
            [
                "Type check error in 'Model.Topic.B' at 4:8-5:14: the class transitively EXTENDS itself.",
                "Type check error in 'Model.Topic.C' at 6:8-7:14: the class transitively EXTENDS itself.",
            ],
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
        => ComparisonRows(nameof(AttributeTest), GetCases(), Wrap);

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
