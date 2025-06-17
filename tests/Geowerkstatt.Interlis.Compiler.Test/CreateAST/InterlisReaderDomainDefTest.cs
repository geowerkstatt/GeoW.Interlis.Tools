using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderDomainDefTest
{
    [TestMethod]
    public void ReadTextDomain()
    {
        AssertReadRule("""
            text = TEXT * 12;
            """,
            new DomainDef
            {
                Name = "text",
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 4 } } },
                TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }, SourceRange = new RangePosition(0, 7, 0, 16), }
            });
    }

    [TestMethod]
    public void ReadDomainWithConstraint()
    {
        AssertReadRule("""
            text = TEXT * 12
                CONSTRAINTS
                    someMath: (1 + 1) == 2; /* (==) has higher precedence than (+) ¯\_(ツ)_/¯ */
                    !!noWhitespaceAtStartAndEnd: INTERLIS.len(INTERLIS.trim(THIS)) == INTERLIS.len(THIS),
                    !!minLength: INTERLIS.len(THIS) > 6;
            """,
            new DomainDef
            {
                Name = "text",
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 4 } } },
                TypeDef = new TextType
                {
                    Length = 12,
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 7, 0, 16),
                    Constraints =
                    {
                        new DomainConstraint
                        {
                            Name = "someMath",
                            Condition = new EqualExpression
                            {
                                FirstOperand = new Addition
                                {
                                    FirstOperand = new NumericConstant { Value = 1 },
                                    SecondOperand = new NumericConstant { Value = 1 }
                                },
                                SecondOperand = new NumericConstant { Value = 2 },
                            }
                        }
                    }
                },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitDomainTypeDef(p.domainTypeDef()));
}
