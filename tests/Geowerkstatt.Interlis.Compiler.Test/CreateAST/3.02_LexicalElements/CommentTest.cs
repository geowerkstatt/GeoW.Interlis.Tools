using Geowerkstatt.Interlis.Compiler.AST;
using System.Text.RegularExpressions;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class CommentTest
{
    // Two kinds of cases, ordered by reference-handbook chapter:
    //  - FullFile cases are complete files; comments standing on their own between top-level tokens are verified
    //    with the main interlis rule (ReadComment).
    //  - Rule cases are modelDef fragments; documentation comments and meta attributes attached to a definition
    //    are verified with the modelDef rule (ReadDefinitionComment) and wrapped into a complete file for the comparison.
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Explanation after model version",
            """
            MODEL Test AT "http://foo.test" VERSION "123" //an explanation// =
            END Test.
            """,
            RefHB: "3.2.6-3",
            Expected: new ModelDef
            {
                Name = "Test",
                NameLocations = { new RangePosition(0, 6, 0, 10), new RangePosition(1, 4, 1, 8) },
                Imports =
                {
                    { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) },
                },
                URI = "http://foo.test",
                Version = "123",
                Explanation = "an explanation",
            }));

        yield return Rule(new(
            "Explanation terminates at first double slash",
            """
            MODEL Test AT "http://foo.test" VERSION "123" //bad // inside// =
            END Test.
            """,
            ExpectedLog: ["Compile error at 1:55-1:61 mismatched input 'inside' expecting {'=', 'TRANSLATION'}."],
            RefHB: "3.2.6-1",
            Expected: new ModelDef
            {
                Name = "Test",
                NameLocations = { new RangePosition(0, 6, 0, 10) },
                Imports =
                {
                    { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) },
                },
                URI = "http://foo.test",
                Version = "123",
                Explanation = "bad ",
            }));

        yield return FullFile(new(
            "Line comment",
            """
            INTERLIS 2.4;
            !! a line comment

            """,
            RefHB: "3.2.8.1-1",
            Expected: new InterlisEnvironment
            {
                Version = 2.4,
                Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } },
            }));

        yield return Rule(new(
            "With meta attributes",
            """
            !!@ key1 = "value with spaces and escapes: \" \\ \u00f8 \uD83D\uDE0E"; key2 = #ff1234/256.0e-10
            MODEL Test AT "http://foo.test" VERSION "123" =
            END Test.
            """,
            RefHB: "3.2.8.1-1",
            Ech0117: "4.2-2",
            Expected: new ModelDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(1, 6, 1, 10),
                    new RangePosition(2, 4, 2, 8)
                },
                MetaAttributes = { { "key1", "value with spaces and escapes: \" \\ ø \U0001F60E" }, { "key2", "#ff1234/256.0e-10" } },
                URI = "http://foo.test",
                Version = "123",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Duplicate meta attributes",
            """
            !!@ KEY_A = red; KEY_B = 1; KEY_B = 2
            !!@ OTHER_KEY = "value"; KEY_A = green
            MODEL Test AT "http://foo.test" VERSION "123" =
            END Test.
            """,
            ExpectedLog: ["Compile error at 3:0-3:5 modelDef has meta attributes with duplicate keys: 'KEY_A', 'KEY_B'."],
            RefHB: "3.2.8.1-1",
            Ech0117: "2.1-3",
            Expected: new ModelDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(2, 6, 2, 10),
                    new RangePosition(3, 4, 3, 8)
                },
                URI = "http://foo.test",
                Version = "123",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            },
            Ili2cDivergenceReason: "we accept duplicate meta attributes (last wins), ili2c rejects them"));

        yield return Rule(new(
            "Unclosed meta comment string",
            Regex.Replace("""
            !!@ key = "value
            MODEL Test AT "http://foo.test" VERSION "123" =
            END Test.
            """, @"(\r\n|\r|\n)", "\r\n"),
            ExpectedLog: [@"Compile error at 1:16-1:18 extraneous input '\r\n' expecting {DOUBLE_QUOTE_CLOSE, LITERAL_NEWLINE, LITERAL_TEXT, '\\', '\""', UNICODE, INVALID_UNICODE, UNKNOWN_ESCAPE}."],
            RefHB: "3.2.8.1-1",
            Ech0117: "4.2-9",
            Expected: new ModelDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(1, 6, 1, 10),
                    new RangePosition(2, 4, 2, 8)
                },
                MetaAttributes = { { "key", "value" } },
                URI = "http://foo.test",
                Version = "123",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return FullFile(new(
            "Block comment",
            """
            INTERLIS 2.4;
            /* Block comment */
            """,
            RefHB: "3.2.8.2-1",
            Expected: new InterlisEnvironment
            {
                Version = 2.4,
                Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } },
            }));

        yield return FullFile(new(
            "Nested block comments",
            """
            INTERLIS 2.4;
            /* Block comment /* nested block comment */ end of Block comment */
            """,
            RefHB: "3.2.8.2-1",
            Expected: new InterlisEnvironment
            {
                Version = 2.4,
                Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } },
            }));

        yield return FullFile(new(
            "Multi-line block comment",
            """
            INTERLIS 2.4;
            /* Block comment
            spanning multiple
            lines */
            """,
            RefHB: "3.2.8.2-1",
            Expected: new InterlisEnvironment
            {
                Version = 2.4,
                Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } },
            }));

        yield return FullFile(new(
            "Block comment containing line comment",
            """
            INTERLIS 2.4;
            /* Block comment
            !! a line comment inside block
            */
            """,
            RefHB: "3.2.8.2-1",
            Expected: new InterlisEnvironment
            {
                Version = 2.4,
                Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } },
            }));

        yield return Rule(new(
            "With documentation comment",
            Regex.Replace("""
            /**
             * Documentation String
             */
            MODEL Test AT "http://foo.test" VERSION "123" =
            END Test.
            """, @"(\r\n|\r|\n)", "\r\n"),
            RefHB: "3.2.8.2-1",
            Expected: new ModelDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(3, 6, 3, 10),
                    new RangePosition(4, 4, 4, 8)
                },
                DocComments = { string.Join("\r\n", "/**", " * Documentation String", " */") },
                URI = "http://foo.test",
                Version = "123",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        {fragment}
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(CommentTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadComment(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitInterlis(p.interlis()));
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadDefinitionComment(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitModelDef(p.modelDef()));
    }
}
