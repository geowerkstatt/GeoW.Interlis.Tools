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
                    new RangePosition { Start = new Position { Line = 0, Character = 6 }, End = new Position { Line = 0, Character = 10 } },
                    new RangePosition { Start = new Position { Line = 1, Character = 4 }, End = new Position { Line = 1, Character = 8 } },
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
                    new RangePosition { Start = new Position { Line = 2, Character = 6 }, End = new Position { Line = 2, Character = 12 } },
                    new RangePosition { Start = new Position { Line = 4, Character = 4 }, End = new Position { Line = 4, Character = 10 } },
                },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                Extends = new Reference<ClassDef> { Path = { "Test_B" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 2, Character = 42 }, End = new Position { Line = 2, Character = 48 } } },
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "UUIDOID" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 3, Character = 11 }, End = new Position { Line = 3, Character = 27 } } },
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
                    new RangePosition { Start = new Position { Line = 0, Character = 6 }, End = new Position { Line = 0, Character = 10 } },
                    new RangePosition { Start = new Position { Line = 3, Character = 4 }, End = new Position { Line = 3, Character = 8 } }
                },
                Content =
                {
                    {
                        "Attr",
                        new AttributeDef
                        {
                            Name = "Attr",
                            NameLocations = { new RangePosition { Start = new Position { Line = 1, Character = 4 }, End = new Position { Line = 1, Character = 8 } } },
                            TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 1, Max = 1 } }
                        }
                    },
                    {
                        "Other",
                        new AttributeDef
                        {
                            Name = "Other",
                            NameLocations = { new RangePosition { Start = new Position { Line = 2, Character = 4 }, End = new Position { Line = 2, Character = 9 } } },
                            TypeDef = new NumericType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 } }
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
                    new RangePosition { Start = new Position { Line = 0, Character = 10 }, End = new Position { Line = 0, Character = 14 } },
                    new RangePosition { Start = new Position { Line = 3, Character = 4 }, End = new Position { Line = 3, Character = 8 } },
                },
                IsStructure = true,
                Content =
                {
                    {
                        "Attr",
                        new AttributeDef
                        {
                            Name = "Attr",
                            NameLocations = { new RangePosition { Start = new Position { Line = 1, Character = 4 }, End = new Position { Line = 1, Character = 8 } } },
                            TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 1, Max = 1 } }
                        }
                    },
                    {
                        "Other",
                        new AttributeDef
                        {
                            Name = "Other",
                            NameLocations = { new RangePosition { Start = new Position { Line = 2, Character = 4 }, End = new Position { Line = 2, Character = 9 } } },
                            TypeDef = new NumericType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 } }
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
