using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderExpressionTest
{
    [TestMethod]
    public void ReadNumericConstExpression()
    {
        AssertReadRule("123", new NumericConstant { Value = 123 });
    }

    [TestMethod]
    public void ReadNumericConstPiExpression()
    {
        AssertReadRule("PI", new NumericConstant { Value = 3.141592653589793 });
    }

    [TestMethod]
    public void ReadTextConstantExpression()
    {
        AssertReadRule(""" "Text Constant \uD83D\uDE0A!" """, new TextConstant { Value = "Text Constant \U0001F60A!" });
    }

    [TestMethod]
    public void ReadEnumerationExpression()
    {
        AssertReadRule("#red.yellow.lightYellow.OTHERS", new EnumerationConstant { Path = { "red", "yellow", "lightYellow", "OTHERS" } });
    }

    [TestMethod]
    public void ReadAttributePathExpression()
    {
        AssertReadRule("THIS->PARENT->class->bag[FIRST]->list[5]",
            new PathExpression
            {
                Path =
                {
                    new KeyWordPathElement { Value = KeyWordPathElement.KeyWord.This },
                    new KeyWordPathElement { Value = KeyWordPathElement.KeyWord.Parent },
                    new IdentifierPathElement { Value = "class" },
                    new IdentifierPathElement { Value = "bag" },
                    new KeyWordPathElement { Value = KeyWordPathElement.KeyWord.First },
                    new IdentifierPathElement { Value = "list" },
                    new IndexerPathElement { Value = 5 },
                }
            });
    }

    [TestMethod]
    public void ReadClassConstExpression()
    {
        AssertReadRule(">Model.Topic.Class",
            new PathExpression
            {
                Path =
                {
                    new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "Model", "Topic", "Class" } } },
                }
            });
    }

    [TestMethod]
    public void ReadAttributePathConstExpression()
    {
        AssertReadRule(">>Model.Topic.Class->Attribute",
            new PathExpression
            {
                Path =
                {
                    new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "Model", "Topic", "Class" } } },
                    new IdentifierPathElement { Value = "Attribute" },
                }
            });
    }

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

    [TestMethod]
    public void ReadFunctionCallExpression()
    {
        AssertReadRule("len(textAttr)",
            new FunctionCall
            {
                FunctionDef = new Reference<FunctionDef> { Path = { "len" } },
                Arguments = { new PathExpression { Path = { new IdentifierPathElement { Value = "textAttr" } } } },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => (IExpression)v.Visit(p.expression()));
}
