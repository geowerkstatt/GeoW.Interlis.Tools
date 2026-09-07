using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class ClassTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Class type attribute",
            "Attr : CLASS;",
            RefHB: "3.8.11-3",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ClassType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 12),
                },
            }));

        yield return Rule(new(
            "Class type with restriction (rule)",
            "ref : CLASS RESTRICTION (A);",
            RefHB: "3.8.11-3",
            Expected: new AttributeDef
            {
                Name = "ref",
                NameLocations = { new RangePosition(0, 0, 0, 3) },
                TypeDef = new ClassType
                {
                    Restrictions = {
                        new Reference<IInterlisDefinition> { Path = { "A" }, SourceRange = new RangePosition(0, 25, 0, 26) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 27),
                },
            }));

        yield return Rule(new(
            "Structure type attribute",
            "Attr : STRUCTURE;",
            RefHB: "3.8.11-3",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new ClassType
                {
                    IsStructure = true,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 16),
                },
            }));

        yield return Rule(new(
            "Structure type with restriction (rule)",
            "ref : STRUCTURE RESTRICTION (A; B);",
            RefHB: "3.8.11-3",
            Expected: new AttributeDef
            {
                Name = "ref",
                NameLocations = { new RangePosition(0, 0, 0, 3) },
                TypeDef = new ClassType
                {
                    IsStructure = true,
                    Restrictions = {
                        new Reference<IInterlisDefinition> { Path = { "A" }, SourceRange = new RangePosition(0, 29, 0, 30) },
                        new Reference<IInterlisDefinition> { Path = { "B" }, SourceRange = new RangePosition(0, 32, 0, 33) },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 6, 0, 34),
                },
            }));

        yield return FullFile(new(
            "Structure type with restriction",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    STRUCTURE A = END A;
                    STRUCTURE B = END B;
                    STRUCTURE Holder =
                        ref : STRUCTURE RESTRICTION (A; B);
                    END Holder;
                END Topic;
            END Model.
            """,
            Description: """
                STRUCTURE RESTRICTION references structures that must exist, so the resolver behaviour
                is also exercised in a full-file context.
                """,
            RefHB: "3.8.11-7",
            AssertOutput: false));

        yield return FullFile(new(
            "Class type with restriction",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS A = END A;
                    CLASS B = END B;
                    CLASS Holder =
                        ref : CLASS RESTRICTION (A; B);
                    END Holder;
                END Topic;
            END Model.
            """,
            Description: """
                RESTRICTION references classes that must exist, so it is exercised in a full-file context
                where both compilers resolve the targets.
                """,
            RefHB: "3.8.11-7",
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
        => ComparisonRows(nameof(ClassTypeTest), GetCases(), Wrap);

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
