using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderExpressionTest
{
    [TestMethod]
    public void ReadAdditionExpression()
    {
        AssertReadRule("1 + 2",
            new Addition
            {
                FirstOperand = new NumericConstant { Value = 1 },
                SecondOperand = new NumericConstant { Value = 2 }
            });
    }

    [TestMethod]
    public void ReadSubtractionExpression()
    {
        AssertReadRule("1 - 2",
            new Subtraction
            {
                FirstOperand = new NumericConstant { Value = 1 },
                SecondOperand = new NumericConstant { Value = 2 }
            });
    }

    [TestMethod]
    public void ReadMultiplicationExpression()
    {
        AssertReadRule("1 * 2",
            new Multiplication
            {
                FirstOperand = new NumericConstant { Value = 1 },
                SecondOperand = new NumericConstant { Value = 2 }
            });
    }

    [TestMethod]
    public void ReadDivisionExpression()
    {
        AssertReadRule("1 / 2",
            new Division
            {
                FirstOperand = new NumericConstant { Value = 1 },
                SecondOperand = new NumericConstant { Value = 2 }
            });
    }

    [TestMethod]
    public void ReadEqualsExpression()
    {
        AssertReadRule("1 == 2",
            new EqualExpression
            {
                FirstOperand = new NumericConstant { Value = 1 },
                SecondOperand = new NumericConstant { Value = 2 }
            });
    }

    [TestMethod]
    public void ReadNotEqualsExpression()
    {
        AssertReadRule("1 != 2",
            new NotExpression
            {
                Operand = new EqualExpression
                {
                    FirstOperand = new NumericConstant { Value = 1 },
                    SecondOperand = new NumericConstant { Value = 2 }
                }
            });
    }

    [TestMethod]
    public void ReadNotEqualsExpressionSqlStyle()
    {
        AssertReadRule("1 <> 2",
            new NotExpression
            {
                Operand = new EqualExpression
                {
                    FirstOperand = new NumericConstant { Value = 1 },
                    SecondOperand = new NumericConstant { Value = 2 }
                }
            });
    }

    [TestMethod]
    public void ReadGreaterThanExpression()
    {
        AssertReadRule("1 > 2",
            new GreaterThanExpression
            {
                FirstOperand = new NumericConstant { Value = 1 },
                SecondOperand = new NumericConstant { Value = 2 }
            });
    }

    [TestMethod]
    public void ReadSmallerThanExpression()
    {
        AssertReadRule("1 < 2",
            new GreaterThanExpression
            {
                FirstOperand = new NumericConstant { Value = 2 },
                SecondOperand = new NumericConstant { Value = 1 }
            });
    }

    [TestMethod]
    public void ReadGreaterThanEqualExpression()
    {
        AssertReadRule("1 >= 2",
            new NotExpression
            {
                Operand = new GreaterThanExpression
                {
                    FirstOperand = new NumericConstant { Value = 2 },
                    SecondOperand = new NumericConstant { Value = 1 }
                }
            });
    }

    [TestMethod]
    public void ReadSmallerThanEqualExpression()
    {
        AssertReadRule("1 <= 2",
            new NotExpression
            {
                Operand = new GreaterThanExpression
                {
                    FirstOperand = new NumericConstant { Value = 1 },
                    SecondOperand = new NumericConstant { Value = 2 }
                }
            });
    }

    [TestMethod]
    public void ReadAndExpression()
    {
        AssertReadRule("(1 == 2) AND (3 == 4)",
            new AndExpression
            {
                FirstOperand = new EqualExpression
                {
                    FirstOperand = new NumericConstant { Value = 1 },
                    SecondOperand = new NumericConstant { Value = 2 }
                },
                SecondOperand = new EqualExpression
                {
                    FirstOperand = new NumericConstant { Value = 3 },
                    SecondOperand = new NumericConstant { Value = 4 }
                },
            });
    }

    [TestMethod]
    public void ReadOrExpression()
    {
        AssertReadRule("(1 == 2) OR (3 == 4)",
            new OrExpression
            {
                FirstOperand = new EqualExpression
                {
                    FirstOperand = new NumericConstant { Value = 1 },
                    SecondOperand = new NumericConstant { Value = 2 }
                },
                SecondOperand = new EqualExpression
                {
                    FirstOperand = new NumericConstant { Value = 3 },
                    SecondOperand = new NumericConstant { Value = 4 }
                },
            });
    }

    [TestMethod]
    public void ReadImplicationExpression()
    {
        AssertReadRule("(1 == 2) => (3 == 4)",
            new OrExpression
            {
                FirstOperand = new NotExpression
                {
                    Operand = new EqualExpression
                    {
                        FirstOperand = new NumericConstant { Value = 1 },
                        SecondOperand = new NumericConstant { Value = 2 }
                    }
                },
                SecondOperand = new EqualExpression
                {
                    FirstOperand = new NumericConstant { Value = 3 },
                    SecondOperand = new NumericConstant { Value = 4 }
                },
            });
    }

    [TestMethod]
    public void ReadNotExpression()
    {
        AssertReadRule("NOT (1 == 2)",
            new NotExpression
            {
                Operand = new EqualExpression
                {
                    FirstOperand = new NumericConstant { Value = 1 },
                    SecondOperand = new NumericConstant { Value = 2 }
                }
            });
    }

    [TestMethod]
    public void ReadDefinedExpression()
    {
        AssertReadRule("DEFINED (UNDEFINED)",
            new DefinedExpression
            {
                Operand = new UndefinedConstant(),
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => (IExpression)v.Visit(p.expression()));
}
