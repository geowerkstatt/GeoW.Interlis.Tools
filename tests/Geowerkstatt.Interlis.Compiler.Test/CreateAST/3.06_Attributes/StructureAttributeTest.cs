using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class StructureAttributeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "List of structure attribute",
            "attr: LIST OF Struct;",
            RefHB: "3.6.4-1",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 14, 0, 20) } },
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound, Ordered = true },
                    SourceRange = new RangePosition(0, 14, 0, 20),
                },
            }));

        yield return Rule(new(
            "Bag of structure attribute",
            "attr: BAG OF Struct;",
            RefHB: "3.6.4-1",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 13, 0, 19) } },
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 13, 0, 19),
                },
            }));

        yield return Rule(new(
            "Concrete structure attribute",
            "attr: Struct;",
            RefHB: "3.6.4-1",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 6, 0, 12) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 12),
                },
            }));

        yield return Rule(new(
            "Bag of structure attribute with cardinality",
            "attr: BAG {1..5} OF Struct;",
            RefHB: "3.6.4-1",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 20, 0, 26) },
                    },
                    Cardinality = new Cardinality { Min = 1, Max = 5 },
                    SourceRange = new RangePosition(0, 20, 0, 26),
                },
            }));

        yield return Rule(new(
            "List of structure attribute with cardinality",
            "attr: LIST {1..*} OF Struct;",
            RefHB: "3.6.4-1",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 21, 0, 27) },
                    },
                    Cardinality = new Cardinality { Min = 1, Max = Cardinality.Unbound, Ordered = true },
                    SourceRange = new RangePosition(0, 21, 0, 27),
                },
            }));

        yield return Rule(new(
            "Structure attribute restriction",
            "attr: Struct RESTRICTION (Sub1);",
            RefHB: "3.6.4-3",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 6, 0, 12) },
                        Restrictions = {
                            new Reference<IInterlisDefinition> { Path = { "Sub1" }, SourceRange = new RangePosition(0, 26, 0, 30) },
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 31),
                },
            }));

        yield return Rule(new(
            "Structure attribute restriction multiple",
            "attr: Struct RESTRICTION (Sub1; Sub2);",
            RefHB: "3.6.4-3",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 6, 0, 12) },
                        Restrictions = {
                            new Reference<IInterlisDefinition> { Path = { "Sub1" }, SourceRange = new RangePosition(0, 26, 0, 30) },
                            new Reference<IInterlisDefinition> { Path = { "Sub2" }, SourceRange = new RangePosition(0, 32, 0, 36) },
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 37),
                },
            }));

        yield return Rule(new(
            "List of structure attribute restriction",
            "attr: LIST OF Struct RESTRICTION (Sub1);",
            RefHB: "3.6.4-3",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 14, 0, 20) },
                        Restrictions = {
                            new Reference<IInterlisDefinition> { Path = { "Sub1" }, SourceRange = new RangePosition(0, 34, 0, 38) },
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound, Ordered = true },
                    SourceRange = new RangePosition(0, 14, 0, 39),
                },
            }));

        yield return Rule(new(
            "Mandatory concrete structure attribute",
            "attr: MANDATORY Struct;",
            RefHB: "3.6.4-4",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 16, 0, 22) },
                    },
                    Cardinality = new Cardinality { Min = 1, Max = 1 },
                    SourceRange = new RangePosition(0, 16, 0, 22),
                },
            }));

        yield return Rule(new(
            "Anystructure attribute",
            "attr: ANYSTRUCTURE;",
            RefHB: "3.6.4-4",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 18),
                },
            }));

        yield return Rule(new(
            "Mandatory anystructure attribute",
            "attr: MANDATORY ANYSTRUCTURE;",
            RefHB: "3.6.4-4",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                    Cardinality = new Cardinality { Min = 1, Max = 1 },
                    SourceRange = new RangePosition(0, 16, 0, 28),
                },
            }));

        yield return Rule(new(
            "Anystructure attribute with restriction",
            "attr: ANYSTRUCTURE RESTRICTION (Struct);",
            RefHB: "3.6.4-4",
            Expected: new AttributeDef
            {
                Name = "attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new UnresolvedNamedType
                {
                    Target = new RestrictedRef
                    {
                        Value = RestrictedRef.AnyKind.Structure,
                        Restrictions = {
                            new Reference<IInterlisDefinition> { Path = { "Struct" }, SourceRange = new RangePosition(0, 32, 0, 38) },
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 39),
                },
            }));

        // Extending a structure attribute across classes needs both the base and the extending class,
        // so it cannot be parsed with the attributeDef rule alone and is only used for the comparison.
        yield return FullFile(new(
            "Extending Struct List as Bag",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    STRUCTURE Struct =
                    END Struct;
                    CLASS BaseClass (ABSTRACT) =
                        attr: LIST OF Struct;
                    END BaseClass;
                    CLASS SubClass EXTENDS BaseClass =
                        attr (EXTENDED): BAG OF Struct;
                    END SubClass;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.SubClass -> attr': a BAG (unordered) can not extend a LIST (ordered)."],
            RefHB: "3.6.4-5",
            AssertOutput: false));

        yield return FullFile(new(
            "Anyclass is not allowed as an attribute type",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        attr: ANYCLASS;
                    END Class;
                END Topic;
            END Model.
            """,
            Description: """
                ANYCLASS is only a class-or-association reference (RefHB 3.6.1-15); it can not stand in for an attribute
                type, which is a domain or a (restricted) structure (RefHB 3.6.1-13).
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.Class -> attr': ANYCLASS is not allowed as an attribute type."],
            RefHB: "3.6.1-12",
            AssertOutput: false));

        yield return FullFile(new(
            "Domain reference can not be restricted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN MyDomain = TEXT*10;
                TOPIC Topic =
                    CLASS Class =
                        attr: MyDomain RESTRICTION (MyDomain);
                    END Class;
                END Topic;
            END Model.
            """,
            Description: """
                A RESTRICTION narrows a structure reference (RefHB 3.6.1-17); a plain domain reference (RefHB 3.8-12)
                can not be restricted.
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.Class -> attr': a domain reference can not be restricted."],
            RefHB: "3.6.1-12",
            AssertOutput: false));

        yield return FullFile(new(
            "Class is not allowed as an attribute type",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Other =
                    END Other;
                    CLASS Class =
                        attr: Other;
                    END Class;
                END Topic;
            END Model.
            """,
            Description: """
                An attribute type is a domain or a (restricted) structure (RefHB 3.6.1-13); a class's objects are only
                reachable through a reference attribute (REFERENCE TO, RefHB 3.6.3).
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.Class -> attr': a class can only be referenced with REFERENCE TO."],
            RefHB: "3.6.1-12",
            AssertOutput: false));

        yield return FullFile(new(
            "Association is not allowed as an attribute type",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS A =
                    END A;
                    CLASS B =
                    END B;
                    ASSOCIATION Assoc =
                        r1 -- {0..*} A;
                        r2 -- {0..*} B;
                    END Assoc;
                    CLASS Class =
                        attr: Assoc;
                    END Class;
                END Topic;
            END Model.
            """,
            Description: "An association is not a value domain either (RefHB 3.6.1-13).",
            ExpectedLog: ["Type check error in 'Model.Topic.Class -> attr': an association is not allowed as an attribute type."],
            RefHB: "3.6.1-12",
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
        => ComparisonRows(nameof(StructureAttributeTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadStructureAttribute(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitAttributeDef(p.attributeDef()));
    }
}
