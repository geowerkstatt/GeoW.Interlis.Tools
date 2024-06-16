using Antlr4.Runtime.Misc;
using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools;

[TestClass]
public class InterlisReaderNumericType
{
    [TestMethod]
    public void ReadNumericAbstract()
    {
        AssertReadRule("NUMERIC", new NumericTypeDef());
    }

    [TestMethod]
    public void ReadNumeric()
    {
        AssertReadRule("000..999",
            new NumericTypeDef
            {
                Min = 0,
                Max = 999,
                Precision = 0,
            });
    }

    [TestMethod]
    public void ReadNumericCircular()
    {
        AssertReadRule("000..999 CIRCULAR",
            new NumericTypeDef
            {
                Min = 0,
                Max = 999,
                Precision = 0,
                Circular = true,
            });
    }

    [TestMethod]
    public void ReadNumericExponential()
    {
        AssertReadRule("0.1e-2 .. 0.20e-1",
            new NumericTypeDef
            {
                Min = 0.001,
                Max = 0.02,
                Precision = -3,
            });
    }

    [TestMethod]
    public void ReadNumericPrecision()
    {
        AssertReadRule("-1.50 .. 10.00",
            new NumericTypeDef
            {
                Min = -1.5,
                Max = 10,
                Precision = -2,
            });
    }

    [TestMethod]
    public void ReadNumericDifferentPrecision()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("0.00 .. 10.0", null));
        Assert.AreEqual("Compile error at line 1:0 Number minimum and maximum must have the same precision but minimum has precision <0.01> and maximum has precision <0.1>.", ex.Message);
    }

    [TestMethod]
    public void ReadNumericMixedExponentialAndDecimal()
    {
        AssertReadRule("0.1e-2 .. 1.000",
            new NumericTypeDef
            {
                Min = 0.001,
                Max = 1,
                Precision = -3,
            });
    }

    [TestMethod]
    public void ReadNumericDoubleInappropriate()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("0.00000 .. 100000000000000.00000", null));
        Assert.AreEqual("Compile error at line 1:0 The given range <0 .. 100000000000000> with a precision of <1E-05> cannot be represented by a double precision floating point number.", ex.Message);
    }

    [TestMethod]
    public void ReadNumericDoubleAppropriate()
    {
        AssertReadRule("0.0 .. 100000000000000.0",
            new NumericTypeDef
            {
                Min = 0,
                Max = 100000000000000,
                Precision = -1,
            });
    }

    [TestMethod]
    public void ReadNumericMinGreaterMax()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("999 .. 000", null));
        Assert.AreEqual("Compile error at line 1:0 Number minimum <999> must be smaller than maximum <0>.", ex.Message);
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitNumericType(p.numericType()));
}
