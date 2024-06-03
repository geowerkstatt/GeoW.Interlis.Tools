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
        AssertReadRule("Attr : TEXT*12;", new AttributeDef { FullyQualifiedName = new Identifier { LeafElementName = "Attr" }, TypeDef = new TypeDef() });
    }

    [TestMethod]
    public void ReadAttributeDefComplete()
    {
        AssertReadRule("""
            !!@ key=value
            /** Doc-Comment */
            CONTINUOUS SUBDIVISION Attr (ABSTRACT, EXTENDED) : 0.00 .. 100.00 := PI, 0.1230e-10;
            """,
            new ClassDef
            {
                FullyQualifiedName = new Identifier { LeafElementName = "Attr" },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } }
            });
    }

    private void AssertReadRule(string input, object? expected)
    {
        var actual = new InterlisReader().ReadRule(new StringReader(input), (p, v) => v.VisitAttributeDef(p.attributeDef()));
        expected.ShouldDeepEqual(actual);
    }
}
