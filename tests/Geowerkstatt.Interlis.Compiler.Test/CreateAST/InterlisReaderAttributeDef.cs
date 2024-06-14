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
    public void ReadAttributeDef()
    {
        AssertReadRule("Attr : TEXT*12;",
            new AttributeDef
            {
                Name = "Attr",
                TypeDef = new TextTypeDef { Length = 12, Cardinality = new Cardinality { Min = 0, Max = 1 } },
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
                TypeDef = new TypeDef { Definition = "0.00..100.00", Cardinality = new Cardinality { Min = 0, Max = 1 } },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitAttributeDef(p.attributeDef()));
}
