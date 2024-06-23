using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools.AST.Expression;
using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools;

[TestClass]
public class InterlisReaderDomainDefTest
{
    [TestMethod]
    public void ReadTextDomain()
    {
        AssertReadRule("""
            text = TEXT * 12;
            """,
            new DomainDef { Name = "text", TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } } } );
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
                TypeDef = new TextType
                {
                    Length = 12,
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
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
