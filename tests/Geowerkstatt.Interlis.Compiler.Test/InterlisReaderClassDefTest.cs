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
            new ClassDef { Name = "Test" });
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
                Name = "Test_A",
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } }
            });
    }

    [TestMethod]
    public void ReadClassWithAttributes()
    {
        AssertReadRule("""
            CLASS Test =
                Attr : MANDATORY TEXT*12;
                Other : 0 .. 100;
            END Test;
            """,
            new ClassDef
            {
                Name = "Test",
                Content =
                {
                    { "Attr", new AttributeDef { Name = "Attr", TypeDef = new TypeDef { Name = "", Definition = "TEXT*12", Cardinality = new Cardinality { Min = 1, Max = 1 } } } },
                    { "Other", new AttributeDef { Name = "Other", TypeDef = new TypeDef { Name = "", Definition = "0..100", Cardinality = new Cardinality { Min = 0, Max = 1 } } } },
                }
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitClassDef(p.classDef()));
}
