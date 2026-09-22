using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class TextTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Text attribute",
            "Attr : TEXT*12;",
            RefHB: "3.8.1-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 14), },
            }));

        yield return Rule(new(
            "Text attribute without length",
            "Attr : TEXT;",
            RefHB: "3.8.1-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType { Length = null, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 11), },
            }));

        yield return Rule(new(
            "MText attribute without length",
            "Attr : MTEXT;",
            RefHB: "3.8.1-3",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    IsMText = true,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 12),
                },
            }));

        yield return Rule(new(
            "MText attribute with length",
            "Attr : MTEXT*20;",
            RefHB: "3.8.1-3",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Length = 20,
                    IsMText = true,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 15),
                },
            }));

        yield return Rule(new(
            "Name attribute",
            "Attr : NAME;",
            RefHB: "3.8.1-6",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Extends = new Reference<DomainDef> { Path = { new("INTERLIS"), new("NAME") } },
                    SourceRange = new RangePosition(0, 7, 0, 11),
                },
            }));

        yield return Rule(new(
            "Uri attribute",
            "Attr : URI;",
            RefHB: "3.8.1-7",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Extends = new Reference<DomainDef> { Path = { new("INTERLIS"), new("URI") } },
                    SourceRange = new RangePosition(0, 7, 0, 10),
                },
            }));

        yield return FullFile(new(
            "Uri and Name domains as final text ranges",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                MyUri (FINAL) = TEXT*1023;
                MyName (FINAL) = TEXT*255;
            END Model.
            """,
            RefHB: "3.8.1-8",
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
        => ComparisonRows(nameof(TextTypeTest), GetCases(), Wrap);

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
