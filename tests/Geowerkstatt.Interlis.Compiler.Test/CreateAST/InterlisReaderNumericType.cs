using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderNumericType
{
    [TestMethod]
    public void ReadNumericAbstract()
    {
        AssertReadRule("NUMERIC", new NumericType { SourceRange = new RangePosition(0, 0, 0, 7) });
    }

    [TestMethod]
    public void ReadNumeric()
    {
        AssertReadRule("000..999",
            new NumericType
            {
                Min = 0,
                Max = 999,
                Precision = 0,
                SourceRange = new RangePosition(0, 0, 0, 8),
            });
    }

    [TestMethod]
    public void ReadNumericCircular()
    {
        AssertReadRule("000..999 CIRCULAR",
            new NumericType
            {
                Min = 0,
                Max = 999,
                Precision = 0,
                Circular = true,
                SourceRange = new RangePosition(0, 0, 0, 17),
            });
    }

    [TestMethod]
    public void ReadNumericExponential()
    {
        AssertReadRule("0.1e-2 .. 0.20e-1",
            new NumericType
            {
                Min = 0.001,
                Max = 0.02,
                Precision = -3,
                SourceRange = new RangePosition(0, 0, 0, 17),
            });
    }

    [TestMethod]
    public void ReadNumericPrecision()
    {
        AssertReadRule("-1.50 .. 10.00",
            new NumericType
            {
                Min = -1.5,
                Max = 10,
                Precision = -2,
                SourceRange = new RangePosition(0, 0, 0, 14),
            });
    }

    [TestMethod]
    public void ReadNumericDifferentPrecision()
    {
        var logs = GetLogMessages("0.00 .. 10.0");
        Assert.AreEqual("Compile error at line 1:0 Number minimum and maximum must have the same precision but minimum has precision <0.01> and maximum has precision <0.1>.", logs.FirstOrDefault());
    }

    [TestMethod]
    public void ReadNumericMixedExponentialAndDecimal()
    {
        AssertReadRule("0.1e-2 .. 1.000",
            new NumericType
            {
                Min = 0.001,
                Max = 1,
                Precision = -3,
                SourceRange = new RangePosition(0, 0, 0, 15),
            });
    }

    [TestMethod]
    public void ReadNumericDoubleInappropriate()
    {
        var logs = GetLogMessages("0.00000 .. 100000000000000.00000");
        Assert.AreEqual("Compile error at line 1:0 The given range <0 .. 100000000000000> with a precision of <1E-05> cannot be represented by a double precision floating point number.", logs.FirstOrDefault());
    }

    [TestMethod]
    public void ReadNumericDoubleAppropriate()
    {
        AssertReadRule("0.0 .. 100000000000000.0",
            new NumericType
            {
                Min = 0,
                Max = 100000000000000,
                Precision = -1,
                SourceRange = new RangePosition(0, 0, 0, 24),
            });
    }

    [TestMethod]
    public void ReadNumericMinGreaterMax()
    {
        var logs = GetLogMessages("999 .. 000");
        Assert.AreEqual("Compile error at line 1:0 Number minimum <999> must be smaller than maximum <0>.", logs.FirstOrDefault());
    }

    [TestMethod]
    public void ReadNumericWithUnit()
    {
        AssertReadRule("0 .. 100 [INTERLIS.m]",
            new NumericType
            {
                Min = 0,
                Max = 100,
                Precision = 0,
                Unit = new Reference<UnitDef>
                {
                    Path = { "INTERLIS", "m" },
                    ReferenceLocation = new RangePosition { Start = new Position { Line = 0, Character = 10 }, End = new Position { Line = 0, Character = 20 } },
                },
                SourceRange = new RangePosition(0, 0, 0, 21),
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitNumericType(p.numericType()));

    private List<string> GetLogMessages(string input)
        => InterlisReaderInterlisFileTest.GetLogMessages(input, (p, v) => v.VisitNumericType(p.numericType()));
}
