using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class BooleanTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Boolean attribute",
            "Attr : BOOLEAN;",
            RefHB: "3.8.4-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 14) },
            }));

        yield return Rule(new(
            "Qualified boolean attribute",
            "Attr : INTERLIS.BOOLEAN;",
            RefHB: "3.8.4-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 23), },
            }));

        yield return Rule(new(
            "Mandatory boolean attribute",
            "Attr : MANDATORY BOOLEAN;",
            RefHB: "3.8.4-4",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 1, Max = 1 },
                    SourceRange = new RangePosition(0, 17, 0, 24),
                },
            }));

        yield return FullFile(new(
            "Boolean domain definition",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN BoolDomain = BOOLEAN;
            END Model.
            """,
            RefHB: "3.8.4-2",
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
        => ComparisonRows(nameof(BooleanTypeTest), GetCases(), Wrap);

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
