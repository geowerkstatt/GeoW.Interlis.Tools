using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class RuntimeParameterTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Runtime parameters",
            "PARAMETER Scale : 0 .. 1000; Detail : 0 .. 9;",
            RefHB: "3.11-3",
            Expected: new List<ParameterDef>
            {
                new ParameterDef
                {
                    Name = "Scale",
                    NameLocations = { new RangePosition(0, 10, 0, 15) },
                    TypeDef = new DecimalType { Min = 0, Max = 1000, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 18, 0, 27) },
                },
                new ParameterDef
                {
                    Name = "Detail",
                    NameLocations = { new RangePosition(0, 29, 0, 35) },
                    TypeDef = new DecimalType { Min = 0, Max = 9, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 38, 0, 44) },
                },
            }));

        yield return Rule(new(
            "Single runtime parameter with numeric range",
            "PARAMETER Scale : 1 .. 1000000;",
            RefHB: "3.11-3",
            Expected: new List<ParameterDef>
            {
                new ParameterDef
                {
                    Name = "Scale",
                    NameLocations = { new RangePosition(0, 10, 0, 15) },
                    TypeDef = new DecimalType { Min = 1, Max = 1000000, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 18, 0, 30) },
                },
            }));

        yield return Rule(new(
            "Runtime parameter with text type",
            "PARAMETER Symbology : TEXT*30;",
            RefHB: "3.11-3",
            Expected: new List<ParameterDef>
            {
                new ParameterDef
                {
                    Name = "Symbology",
                    NameLocations = { new RangePosition(0, 10, 0, 19) },
                    TypeDef = new TextType { Length = 30, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 22, 0, 29) },
                },
            }));

        yield return Rule(new(
            "Runtime parameter with boolean type",
            "PARAMETER Active : BOOLEAN;",
            RefHB: "3.11-3",
            Expected: new List<ParameterDef>
            {
                new ParameterDef
                {
                    Name = "Active",
                    NameLocations = { new RangePosition(0, 10, 0, 16) },
                    TypeDef = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 19, 0, 26) },
                },
            }));

        yield return Rule(new(
            "Runtime parameter with enumeration type",
            "PARAMETER Mode : (draft, final);",
            RefHB: "3.11-3",
            Expected: new List<ParameterDef>
            {
                new ParameterDef
                {
                    Name = "Mode",
                    NameLocations = { new RangePosition(0, 10, 0, 14) },
                    TypeDef = new EnumerationType
                    {
                        Values =
                        {
                            new EnumerationTreeNode { Name = "draft" },
                            new EnumerationTreeNode { Name = "final" },
                        },
                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                        SourceRange = new RangePosition(0, 17, 0, 31),
                    },
                },
            }));

        yield return Rule(new(
            "Runtime parameter with AREA type (currently accepted)",
            "PARAMETER Region : AREA;",
            Description: """
                AREA is not permitted for runtime parameters. The compiler does not yet reject it (parses to a
                SurfaceType), so this documents the current behaviour.
                """,
            RefHB: "3.11-4",
            Expected: new List<ParameterDef>
            {
                new ParameterDef
                {
                    Name = "Region",
                    NameLocations = { new RangePosition(0, 10, 0, 16) },
                    TypeDef = new SurfaceType { IsCoverage = true, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 19, 0, 23) },
                },
            }));

        yield return Rule(new(
            "Runtime parameter for current date",
            "PARAMETER CurrentDate : FORMAT INTERLIS.XMLDate \"2000-01-01\" .. \"2099-12-31\";",
            Description: "Typical runtime parameter is the current date.",
            RefHB: "3.11-5",
            Expected: new List<ParameterDef>
            {
                new ParameterDef
                {
                    Name = "CurrentDate",
                    NameLocations = { new RangePosition(0, 10, 0, 21) },
                    TypeDef = new FormattedType
                    {
                        FormatBaseType = new Reference<DomainDef> { Path = { "INTERLIS", "XMLDate" }, SourceRange = new RangePosition(0, 31, 0, 47) },
                        Min = "2000-01-01",
                        Max = "2099-12-31",
                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                        SourceRange = new RangePosition(0, 24, 0, 76),
                    },
                },
            }));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            {fragment}
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(RuntimeParameterTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadRunTimeParameterDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitRunTimeParameterDef(p.runTimeParameterDef()));
    }
}
