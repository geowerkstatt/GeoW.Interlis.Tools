using Antlr4.Runtime.Misc;
using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools.AST.Types;

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
                MetaAttributes = { { "key", "value" } },
                Extends = new Reference<ClassDef> { Path = { "Test_B" } },
                OidType = new Reference<TypeDef> { Path = { "INTERLIS", "UUIDOID" } },
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
                    { "Attr", new AttributeDef { Name = "Attr", TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 1, Max = 1 } } } },
                    { "Other", new AttributeDef { Name = "Other", TypeDef = new NumericType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 } } } },
                }
            });
    }

    [TestMethod]
    public void ReadStructure()
    {
        AssertReadRule("""
            STRUCTURE Test =
                Attr : MANDATORY TEXT*12;
                Other : 0 .. 100;
            END Test;
            """,
            new ClassDef
            {
                Name = "Test",
                IsStructure = true,
                Content =
                {
                    { "Attr", new AttributeDef { Name = "Attr", TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 1, Max = 1 } } } },
                    { "Other", new AttributeDef { Name = "Other", TypeDef = new NumericType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 } } } },
                }
            });
    }

    [TestMethod]
    public void ReadStructureWithOid()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() => AssertReadRule("""
            STRUCTURE Test =
                OID AS INTERLIS.UUIDOID;
            END Test;
            """, null));
        Assert.AreEqual("Compile error at line 2:4 Structure 'Test' cannot have an OID definition.", ex.Message);
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitClassDef(p.classDef()));
}
