using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class AttributePathTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Attribute path type",
            "Attr : ATTRIBUTE;",
            RefHB: "3.8.11-4",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new AttributePathType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 16),
                },
            }));

        yield return Rule(new(
            "Attribute path type with restriction",
            "Attr : ATTRIBUTE RESTRICTION (TEXT);",
            RefHB: "3.8.11-4",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new AttributePathType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Restrictions =
                    {
                        new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 30, 0, 34) },
                    },
                    SourceRange = new RangePosition(0, 7, 0, 35),
                },
            }));

        yield return Rule(new(
            "Attribute path type OF class",
            "Attr : ATTRIBUTE OF SomeClass;",
            RefHB: "3.8.11-4",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new AttributePathType
                {
                    Of = new PathExpression
                    {
                        Reference = new Reference<IInterlisDefinition> { Path =
                        {
                            new("SomeClass"),
                        } },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 29),
                },
            }));

        yield return Rule(new(
            "Attribute path type OF class attribute path",
            "Attr : ATTRIBUTE OF OtherClass->SomeAttr;",
            RefHB: "3.8.11-4",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new AttributePathType
                {
                    Of = new PathExpression
                    {
                        Reference = new Reference<IInterlisDefinition> { Path =
                        {
                            new("OtherClass"),
                            new("SomeAttr"),
                        } },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 40),
                },
            }));

        yield return Rule(new(
            "Attribute path type OF class with restriction",
            "Attr : ATTRIBUTE OF SomeClass RESTRICTION (TEXT);",
            RefHB: "3.8.11-4",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new AttributePathType
                {
                    Of = new PathExpression
                    {
                        Reference = new Reference<IInterlisDefinition> { Path =
                        {
                            new("SomeClass"),
                        } },
                    },
                    Restrictions =
                    {
                        new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 43, 0, 47) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 48),
                },
            }));

        yield return Rule(new(
            "Attribute path type with multiple restrictions",
            "Attr : ATTRIBUTE RESTRICTION (TEXT; NUMERIC);",
            RefHB: "3.8.11-4",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new AttributePathType
                {
                    Restrictions =
                    {
                        new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 30, 0, 34) },
                        new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 36, 0, 43) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 44),
                },
            }));
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

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(AttributePathTypeTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadAttributeDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitAttributeDef(p.attributeDef()));
    }
}
