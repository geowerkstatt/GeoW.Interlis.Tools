using Antlr4.Runtime.Misc;
using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools;

[TestClass]
public class InterlisReaderCardinalityTest
{
    [TestMethod]
    public void ReadCardinality()
    {
        AssertReadRule("{0..*}", new Cardinality { Min = 0, Max = Cardinality.Unbound });
    }

    [TestMethod]
    public void ReadCardinalityStar()
    {
        AssertReadRule("{*}", new Cardinality { Min = 0, Max = Cardinality.Unbound });
    }

    [TestMethod]
    public void ReadCardinalityConstant()
    {
        AssertReadRule("{42}", new Cardinality { Min = 42, Max = 42 });
    }

    [TestMethod]
    public void ReadCardinalityRange()
    {
        AssertReadRule("{3..5}", new Cardinality { Min = 3, Max = 5 });
    }

    [TestMethod]
    public void ReadCardinalityInvalidStar()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("{*..5}", null));
        Assert.AreEqual("Compile error at line 1:1 Invalid cardinality '{*..5}', did you mean '{0..5}'.", ex.Message);
    }

    [TestMethod]
    public void ReadCardinalityInvalidStarStar()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("{*..*}", null));
        Assert.AreEqual("Compile error at line 1:1 Invalid cardinality '{*..*}', did you mean '{0..*}'.", ex.Message);
    }

    [TestMethod]
    public void ReadCardinalityRangeSwapped()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("{8..1}", null));
        Assert.AreEqual("Compile error at line 1:0 Invalid cardinality minimal value '8' is larger than maximal value '1'.", ex.Message);
    }

    [TestMethod]
    public void ReadCardinalityTooLarge()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule($"{{0..9223372036854775808}}", null));
        Assert.AreEqual("Compile error at line 1:4 Could not parse value 9223372036854775808.", ex.Message);
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitCardinality(p.cardinality()));
}
