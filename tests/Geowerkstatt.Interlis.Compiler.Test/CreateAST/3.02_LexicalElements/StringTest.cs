using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class StringTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Empty string",
            "\"\"",
            RefHB: "3.2.3-4",
            Expected: ""));

        yield return Rule(new(
            "Plain string without escapes",
            "\"plain text 123\"",
            RefHB: "3.2.3-4",
            Expected: "plain text 123"));

        yield return Rule(new(
            "Escaped quote",
            "\"\\\"\"",
            RefHB: "3.2.3-1",
            Expected: "\""));

        yield return Rule(new(
            "Escaped backslash",
            "\"\\\\\"",
            RefHB: "3.2.3-1",
            Expected: "\\"));

        yield return Rule(new(
            "Read String",
            "\"value with spaces and escapes: \\\" \\\\ \\u00f8 \\uD83D\\uDE0E\"",
            RefHB: "3.2.3-1",
            Expected: "value with spaces and escapes: \" \\ ø \U0001F60E"));

        yield return Rule(new(
            "Invalid Escape",
            "\"invalid escape: \\n \"",
            ExpectedLog: [@"Compile error at 1:17-1:19 Invalid escape sequence inside String: '\n'."],
            RefHB: "3.2.3-1",
            Expected: "invalid escape: \\n "));

        yield return Rule(new(
            "Invalid Escape at end of line",
            "\"invalid unicode: \\\r\nmore text\"",
            ExpectedLog: [
                "Compile error at 1:18-1:19 Invalid escape sequence inside String: '\\'.",
                "Compile error at 1:19-1:21 Strings cannot span multiple lines.",
            ],
            RefHB: "3.2.3-1",
            Expected: "invalid unicode: \\\r\nmore text"));

        yield return Rule(new(
            "Missing closing quotes",
            "\"Text without closing quotes",
            ExpectedLog: [@"Compile error at 1:28-1:29 extraneous input '<EOF>' expecting {DOUBLE_QUOTE_CLOSE, LITERAL_NEWLINE, LITERAL_TEXT, '\\', '\""', UNICODE, INVALID_UNICODE, UNKNOWN_ESCAPE}."],
            RefHB: "3.2.3-1",
            Expected: "Text without closing quotes",
            Ili2cDivergenceReason: "ili2c hangs on an unterminated string literal (hits the compile timeout); our compiler rejects it with a parse error (RefHB 3.2.3)"));

        yield return Rule(new(
            "String over multiple lines",
            "\"first line\r\nsecond line\"",
            ExpectedLog: ["Compile error at 1:11-1:13 Strings cannot span multiple lines."],
            RefHB: "3.2.3-1",
            Expected: "first line\r\nsecond line",
            Ili2cDivergenceReason: "we reject strings spanning multiple lines (RefHB 3.2.3), ili2c accepts them"));

        yield return Rule(new(
            "Unicode escape four hex digits",
            "\"\\u00e4\"",
            RefHB: "3.2.3-2",
            Expected: "ä"));

        yield return Rule(new(
            "Unicode surrogate pair above U+10000",
            "\"\\uD83D\\uDE00\"",
            RefHB: "3.2.3-2",
            Expected: "😀"));

        yield return Rule(new(
            "Invalid Unicode",
            "\"invalid escape: \\udefg \"",
            ExpectedLog: [@"Compile error at 1:17-1:23 Unicode escape sequence with invalid characters: '\udefg'."],
            RefHB: "3.2.3-2",
            Expected: "invalid escape: \\udefg "));

        yield return Rule(new(
            "Invalid Unicode at end of String",
            "\"invalid escape: \\ug\"",
            ExpectedLog: [@"Compile error at 1:17-1:20 Unicode escape sequence with invalid characters: '\ug'."],
            RefHB: "3.2.3-2",
            Expected: "invalid escape: \\ug"));

        yield return Rule(new(
            "Invalid Unicode before other Unicode",
            "\"invalid escape: \\u123\\u00f8\"",
            ExpectedLog: [@"Compile error at 1:17-1:22 Unicode escape sequence with invalid characters: '\u123'."],
            RefHB: "3.2.3-2",
            Expected: "invalid escape: \\u123ø"));

        yield return Rule(new(
            "Invalid too short Unicode escape at end of String",
            "\"\\u048\"",
            ExpectedLog: [@"Compile error at 1:1-1:6 Unicode escape sequence with invalid characters: '\u048'."],
            RefHB: "3.2.3-2",
            Expected: "\\u048"));

        yield return Rule(new(
            "Invalid too short Unicode escape at end of line",
            "\"invalid unicode: \\u1\r\nmore text\"",
            ExpectedLog: [
                "Compile error at 1:18-1:21 Unicode escape sequence with invalid characters: '\\u1'.",
                "Compile error at 1:21-1:23 Strings cannot span multiple lines.",
            ],
            RefHB: "3.2.3-2",
            Expected: "invalid unicode: \\u1\r\nmore text"));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION {fragment} =
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(StringTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadString(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitString(p.@string()));
    }
}
