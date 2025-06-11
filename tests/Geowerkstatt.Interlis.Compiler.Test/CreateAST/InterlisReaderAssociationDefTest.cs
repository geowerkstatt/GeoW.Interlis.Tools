using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderAssociationDefTest
{
    [TestMethod]
    public void ReadAssociationDef()
    {
        AssertReadRule("""
            ASSOCIATION Test =
            END Test;
            """, new AssociationDef { Name = "Test", Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } });
    }

    [TestMethod]
    public void ReadAssociationDefComplete()
    {
        AssertReadRule("""
            ASSOCIATION Test_A (ABSTRACT, EXTENDED) EXTENDS Test_B DERIVED FROM Test_C =
                OID AS oidType;
                ATTRIBUTE
                CARDINALITY = {5 .. 42} ;
            END Test_A;
            """,
            new AssociationDef
            {
                Name = "Test_A",
                Cardinality = new Cardinality { Min = 5, Max = 42 },
                Extends = new Reference<AssociationDef> { Path = { "Test_B" } },
                OidType = new Reference<TypeDef> { Path = { "oidType" } },
                Properties = { Property.Abstract, Property.Extended },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitAssociationDef(p.associationDef()));
}
