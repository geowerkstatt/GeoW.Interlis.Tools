using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools.AST;

namespace Geowerkstatt.Interlis.Tools;

[TestClass]
public class InterlisReaderAssociationDefTest
{
    [TestMethod]
    public void ReadAssociationDef()
    {
        AssertReadRule("""
            ASSOCIATION Test =
            END Test;
            """, new AssociationDef { FullyQualifiedName = new Identifier { Class = "Test" } });
    }

    [TestMethod]
    public void ReadAssociationDefComplete()
    {
        AssertReadRule("""
            ASSOCIATION Test_A (ABSTRACT, EXTENDED) EXTENDS Test_B DERIVED FROM Test_C =
                OID AS oidType;
                ATTRIBUTE
                CARDINALITY = {0 .. *} ;
            END Test_A;
            """,
            new AssociationDef
            {
                FullyQualifiedName = new Identifier { Class = "Test_A" },
            });
    }

    private void AssertReadRule(string input, object? expected)
    {
        var actual = new InterlisReader().ReadRule(new StringReader(input), (p, v) => v.VisitAssociationDef(p.associationDef()));
        expected.ShouldDeepEqual(actual);
    }
}
