using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools;

[TestClass]
public class InterlisReaderClassDefTest
{
    [TestMethod]
    public void ReadClassDef()
    {
        AssertReadRule("""
            CLASS Test =
            END Test;
            """,
            new ClassDef { FullyQualifiedName = new Identifier { Class = "Test" } });
    }

    [TestMethod]
    public void ReadClassDefComplete()
    {
        AssertReadRule("""
            !!@ key=value
            /** Doc-Comment */
            CLASS Test_A (ABSTRACT, EXTENDED) EXTENDS Test_B =
                OID AS INTERLIS.UUIDOID;
            END Test_A;
            """,
            new ClassDef
            {
                FullyQualifiedName = new Identifier { Class = "Test_A" },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } }
            });
    }

    private void AssertReadRule(string input, object? expected)
    {
        var actual = new InterlisReader().ReadRule(new StringReader(input), (p, v) => v.VisitClassDef(p.classDef()));
        expected.ShouldDeepEqual(actual);
    }
}
