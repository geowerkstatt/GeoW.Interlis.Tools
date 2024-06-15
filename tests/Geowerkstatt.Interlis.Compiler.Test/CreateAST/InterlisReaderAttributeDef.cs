using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools;

[TestClass]
public class InterlisReaderAttributeDef
{
    [TestMethod]
    public void ReadTextAttributeDef()
    {
        AssertReadRule("Attr : TEXT*12;",
            new AttributeDef
            {
                Name = "Attr",
                TypeDef = new TextTypeDef { Length = 12, Cardinality = new Cardinality { Min = 0, Max = 1 } },
            });
    }

    [TestMethod]
    public void ReadNumericAttributeDef()
    {
        AssertReadRule("Attr : 000..999;",
            new AttributeDef
            {
                Name = "Attr",
                TypeDef = new NumericTypeDef { Min = 0, Max = 999, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 } },
            });
    }

    [TestMethod]
    public void ReadBooleanAttributeDef()
    {
        AssertReadRule("Attr : BOOLEAN;",
            new AttributeDef
            {
                Name = "Attr",
                TypeDef = new BooleanTypeDef { Cardinality = new Cardinality { Min = 0, Max = 1 } },
            });
    }

    [TestMethod]
    public void ReadAttributeDefComplete()
    {
        AssertReadRule("""
            !!@ key=value
            /** Doc-Comment */
            CONTINUOUS SUBDIVISION Attr (ABSTRACT, EXTENDED) : 0.00 .. 100.00 := PI, 0.1230e-10;
            """,
            new AttributeDef
            {
                Name = "Attr",
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                TypeDef = new NumericTypeDef { Min = 0, Max = 100, Precision = -2, Cardinality = new Cardinality { Min = 0, Max = 1 } },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitAttributeDef(p.attributeDef()));
}
