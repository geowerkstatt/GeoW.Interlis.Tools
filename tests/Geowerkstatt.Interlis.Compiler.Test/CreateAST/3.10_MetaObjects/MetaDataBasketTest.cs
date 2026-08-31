using Geowerkstatt.Interlis.Compiler.AST;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class MetaDataBasketTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Sign meta data basket",
            "SIGN BASKET B ~ T OBJECTS OF C : a, b;",
            Description: """
                The comparison diverges: a meta-data basket references a topic and signature classes that must conform
                to ili2c's predefined SIGN / METAOBJECT model, which this compiler does not model yet. The AST is verified
                by the rule-level ReadMetaDataBasketDef test; the comparison is left red as a known compiler-gap signal.
                """,
            RefHB: "3.10.1-6",
            Expected: new MetaDataBasketDef
            {
                Name = "B",
                NameLocations = { new RangePosition(0, 12, 0, 13) },
                Kind = MetaDataBasketDef.BasketKind.Sign,
                Topic = new Reference<TopicDef> { Path = { "T" }, SourceRange = new RangePosition(0, 16, 0, 17) },
                Objects =
                {
                    new MetaObjectsClause { Class = new Reference<ClassDef> { Path = { "C" }, SourceRange = new RangePosition(0, 29, 0, 30) }, MetaObjects = { new MetaObjectDeclaration { Name = "a", NameLocations = { new RangePosition(0, 33, 0, 34) } }, new MetaObjectDeclaration { Name = "b", NameLocations = { new RangePosition(0, 36, 0, 37) } } } },
                },
            }));

        yield return Rule(new(
            "Refsystem meta data basket",
            "REFSYSTEM BASKET Coordinates ~ ReferenceSystems;",
            RefHB: "3.10.1-6",
            Expected: new MetaDataBasketDef
            {
                Name = "Coordinates",
                NameLocations = { new RangePosition(0, 17, 0, 28) },
                Kind = MetaDataBasketDef.BasketKind.Refsystem,
                Topic = new Reference<TopicDef> { Path = { "ReferenceSystems" }, SourceRange = new RangePosition(0, 31, 0, 47) },
            }));

        yield return Rule(new(
            "Final meta data basket",
            "SIGN BASKET B (FINAL) ~ T;",
            RefHB: "3.10.1-6",
            Expected: new MetaDataBasketDef
            {
                Name = "B",
                NameLocations = { new RangePosition(0, 12, 0, 13) },
                Properties = { Property.Final },
                Kind = MetaDataBasketDef.BasketKind.Sign,
                Topic = new Reference<TopicDef> { Path = { "T" }, SourceRange = new RangePosition(0, 24, 0, 25) },
            }));

        yield return Rule(new(
            "Refsystem basket single meta object name",
            "REFSYSTEM BASKET B ~ T OBJECTS OF C : single;",
            RefHB: "3.10.1-6",
            Expected: new MetaDataBasketDef
            {
                Name = "B",
                NameLocations = { new RangePosition(0, 17, 0, 18) },
                Kind = MetaDataBasketDef.BasketKind.Refsystem,
                Topic = new Reference<TopicDef> { Path = { "T" }, SourceRange = new RangePosition(0, 21, 0, 22) },
                Objects =
                {
                    new MetaObjectsClause { Class = new Reference<ClassDef> { Path = { "C" }, SourceRange = new RangePosition(0, 34, 0, 35) }, MetaObjects = { new MetaObjectDeclaration { Name = "single", NameLocations = { new RangePosition(0, 38, 0, 44) } } } },
                },
            }));

        yield return Rule(new(
            "Meta data basket with multiple objects-of clauses",
            "SIGN BASKET B ~ T OBJECTS OF C1 : a OBJECTS OF C2 : b, c;",
            RefHB: "3.10.1-6",
            Expected: new MetaDataBasketDef
            {
                Name = "B",
                NameLocations = { new RangePosition(0, 12, 0, 13) },
                Kind = MetaDataBasketDef.BasketKind.Sign,
                Topic = new Reference<TopicDef> { Path = { "T" }, SourceRange = new RangePosition(0, 16, 0, 17) },
                Objects =
                {
                    new MetaObjectsClause { Class = new Reference<ClassDef> { Path = { "C1" }, SourceRange = new RangePosition(0, 29, 0, 31) }, MetaObjects = { new MetaObjectDeclaration { Name = "a", NameLocations = { new RangePosition(0, 34, 0, 35) } } } },
                    new MetaObjectsClause { Class = new Reference<ClassDef> { Path = { "C2" }, SourceRange = new RangePosition(0, 47, 0, 49) }, MetaObjects = { new MetaObjectDeclaration { Name = "b", NameLocations = { new RangePosition(0, 52, 0, 53) } }, new MetaObjectDeclaration { Name = "c", NameLocations = { new RangePosition(0, 55, 0, 56) } } } },
                },
            }));

        yield return Rule(new(
            "Meta data basket extends simple basket ref",
            "SIGN BASKET B EXTENDS Base ~ T;",
            RefHB: "3.10.1-7",
            Expected: new MetaDataBasketDef
            {
                Name = "B",
                NameLocations = { new RangePosition(0, 12, 0, 13) },
                Kind = MetaDataBasketDef.BasketKind.Sign,
                Extends = new Reference<MetaDataBasketDef> { Path = { "Base" }, SourceRange = new RangePosition(0, 22, 0, 26) },
                Topic = new Reference<TopicDef> { Path = { "T" }, SourceRange = new RangePosition(0, 29, 0, 30) },
            }));

        yield return Rule(new(
            "Meta data basket extends qualified basket ref",
            "SIGN BASKET B EXTENDS BaseModel.BaseTopic.BaseBasket ~ T;",
            RefHB: "3.10.1-7",
            Expected: new MetaDataBasketDef
            {
                Name = "B",
                NameLocations = { new RangePosition(0, 12, 0, 13) },
                Kind = MetaDataBasketDef.BasketKind.Sign,
                Extends = new Reference<MetaDataBasketDef> { Path = { "BaseModel", "BaseTopic", "BaseBasket" }, SourceRange = new RangePosition(0, 22, 0, 52) },
                Topic = new Reference<TopicDef> { Path = { "T" }, SourceRange = new RangePosition(0, 55, 0, 56) },
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
        => ComparisonRows(nameof(MetaDataBasketTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadMetaDataBasketDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitMetaDataBasketDef(p.metaDataBasketDef()));
    }
}
