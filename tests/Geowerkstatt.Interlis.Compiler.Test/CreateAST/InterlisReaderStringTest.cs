using Antlr4.Runtime.Misc;
using DeepEqual.Syntax;

namespace Geowerkstatt.Interlis.Tools;

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
        Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("\"invalid escape: \\udefg \"", null));
    }

    [TestMethod]
    public void ReadStringInvalidEscape()
    {
        Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("\"invalid escape: \\n \"", null));
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitString(p.@string()));
}
