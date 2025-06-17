using Antlr4.Runtime.Misc;
using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler;

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
            new ClassDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 10),
                    new RangePosition(1, 4, 1, 8),
                },
            });
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
                NameLocations =
                {
                    new RangePosition(2, 6, 2, 12),
                    new RangePosition(4, 4, 4, 10),
                },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                Extends = new Reference<ClassDef> { Path = { "Test_B" }, ReferenceLocation = new RangePosition(2, 42, 2, 48) },
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "UUIDOID" }, ReferenceLocation = new RangePosition(3, 11, 3, 27) },
                Properties = { Property.Abstract, Property.Extended },
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
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 10),
                    new RangePosition(3, 4, 3, 8)
                },
                Content =
                {
                    {
                        "Attr",
                        new AttributeDef
                        {
                            Name = "Attr",
                            NameLocations = { new RangePosition(1, 4, 1, 8) },
                            TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 1, Max = 1 }, SourceRange = new RangePosition(1, 21, 1, 28), }
                        }
                    },
                    {
                        "Other",
                        new AttributeDef
                        {
                            Name = "Other",
                            NameLocations = { new RangePosition(2, 4, 2, 9) },
                            TypeDef = new NumericType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 12, 2, 20), }
                        }
                    },
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
                NameLocations =
                {
                    new RangePosition(0, 10, 0, 14),
                    new RangePosition(3, 4, 3, 8),
                },
                IsStructure = true,
                Content =
                {
                    {
                        "Attr",
                        new AttributeDef
                        {
                            Name = "Attr",
                            NameLocations = { new RangePosition(1, 4, 1, 8) },
                            TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 1, Max = 1 }, SourceRange = new RangePosition(1, 21, 1, 28), }
                        }
                    },
                    {
                        "Other",
                        new AttributeDef
                        {
                            Name = "Other",
                            NameLocations = { new RangePosition(2, 4, 2, 9) },
                            TypeDef = new NumericType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 12, 2, 20), }
                        }
                    },
                }
            });
    }

    [TestMethod]
    public void ReadStructureWithOid()
    {
        var logs = GetLogMessages("""
            STRUCTURE Test =
                OID AS INTERLIS.UUIDOID;
            END Test;
            """);
        Assert.AreEqual("Compile error at line 2:4 Structure 'Test' cannot have an OID definition.", logs.FirstOrDefault());
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitClassDef(p.classDef()));

    private List<string> GetLogMessages(string input)
        => InterlisReaderInterlisFileTest.GetLogMessages(input, (p, v) => v.VisitClassDef(p.classDef()));
}
