using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class BlackboxTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Blackbox XML attribute",
            "Attr : BLACKBOX XML;",
            RefHB: "3.8.10-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BlackboxType
                {
                    Kind = BlackboxType.BlackboxTypeKind.Xml,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 19),
                },
            }));

        yield return Rule(new(
            "Blackbox binary attribute",
            "Attr : BLACKBOX BINARY;",
            RefHB: "3.8.10-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BlackboxType
                {
                    Kind = BlackboxType.BlackboxTypeKind.Binary,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 22),
                },
            }));

        yield return Rule(new(
            "Blackbox rejects invalid variant",
            "Attr : BLACKBOX TEXT;",
            ExpectedLog: ["Compile error at line 1:16 mismatched input 'TEXT' expecting {'BINARY', 'XML'}."],
            RefHB: "3.8.10-3",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BlackboxType
                {
                    Kind = BlackboxType.BlackboxTypeKind.Binary,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 20),
                },
            }));

        yield return FullFile(new(
            "Blackbox XML and binary attributes in class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
                CLASS ClassName =
                  Doc : BLACKBOX XML;
                  Blob : BLACKBOX BINARY;
                END ClassName;
              END Topic;
            END Model.
            """,
            RefHB: "3.8.10-3",
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
        => ComparisonRows(nameof(BlackboxTypeTest), GetCases(), Wrap);

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
