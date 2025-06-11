using Antlr4.Runtime.Misc;
using DeepEqual.Syntax;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderStringTest
{
    [TestMethod]
    public void ReadString()
    {
        AssertReadRule("""
            "value with spaces and escapes: \" \\ \u00f8 \uD83D\uDE0E"
            """,
            "value with spaces and escapes: \" \\ ø \U0001F60E");
    }

    [TestMethod]
    public void ReadStringInvalidUnicode()
    {
        var logs = GetLogMessages("\"invalid escape: \\udefg \"");
        Assert.IsTrue(logs.Count > 0);
    }

    [TestMethod]
    public void ReadStringInvalidEscape()
    {
        var logs = GetLogMessages("\"invalid escape: \\n \"");
        Assert.IsTrue(logs.Count > 0);
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitString(p.@string()));

    private List<string> GetLogMessages(string input)
        => InterlisReaderInterlisFileTest.GetLogMessages(input, (p, v) => v.VisitString(p.@string()));
}
