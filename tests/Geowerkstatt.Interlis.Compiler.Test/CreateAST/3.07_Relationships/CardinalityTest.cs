using Geowerkstatt.Interlis.Compiler.AST;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class CardinalityTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Cardinality range to unbound",
            "{0..*}",
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 0, Max = Cardinality.Unbound }));

        yield return Rule(new(
            "Cardinality star",
            "{*}",
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 0, Max = Cardinality.Unbound }));

        yield return Rule(new(
            "Cardinality constant",
            "{42}",
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 42, Max = 42 }));

        yield return Rule(new(
            "Cardinality range",
            "{3..5}",
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 3, Max = 5 }));

        yield return Rule(new(
            "Cardinality lower bound to unbound",
            "{1..*}",
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 1, Max = Cardinality.Unbound }));

        yield return Rule(new(
            "Cardinality with invalid star minimum",
            "{*..5}",
            ExpectedLog: ["Compile error at line 1:1 Invalid cardinality '{*..5}', did you mean '{0..5}'."],
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 0, Max = 5 }));

        yield return Rule(new(
            "Cardinality with invalid star minimum and maximum",
            "{*..*}",
            ExpectedLog: ["Compile error at line 1:1 Invalid cardinality '{*..*}', did you mean '{0..*}'."],
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 0, Max = Cardinality.Unbound }));

        yield return Rule(new(
            "Cardinality with swapped range",
            "{8..1}",
            ExpectedLog: ["Compile error at line 1:0 Invalid cardinality minimal value '8' is larger than maximal value '1'."],
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 1, Max = 8 }));

        yield return Rule(new(
            "Cardinality with too large value",
            "{0..9223372036854775808}",
            ExpectedLog: ["Compile error at line 1:4 Could not parse value 9223372036854775808."],
            RefHB: "3.7.3-1",
            Expected: new Cardinality { Min = 0, Max = Cardinality.Unbound }));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                CLASS ClassName =
                    attr : BAG {fragment} OF ANYSTRUCTURE;
                END ClassName;
            END Topic;
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(CardinalityTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadCardinality(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitCardinality(p.cardinality()));
    }
}
