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
            """,
            new AssociationDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition { Start = new Position { Line = 0, Character = 12 }, End = new Position { Line = 0, Character = 16 } },
                    new RangePosition { Start = new Position { Line = 1, Character = 4 }, End = new Position { Line = 1, Character = 8 } },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            });
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
                NameLocations =
                {
                    new RangePosition { Start = new Position { Line = 0, Character = 12 }, End = new Position { Line = 0, Character = 18 } },
                    new RangePosition { Start = new Position { Line = 4, Character = 4 }, End = new Position { Line = 4, Character = 10 } },
                },
                Cardinality = new Cardinality { Min = 5, Max = 42 },
                Extends = new Reference<AssociationDef> { Path = { "Test_B" } },
                OidType = new Reference<TypeDef> { Path = { "oidType" } },
                Properties = { Property.Abstract, Property.Extended },
            });
    }

    [TestMethod]
    public void ReadAssociationWithoutName()
    {
        AssertReadRule("""
            ASSOCIATION =
                Document -- DocumentClass;
                Action -- ActionClass;
            END;
            """,
            new AssociationDef
            {
                Name = "DocumentAction",
                NameLocations = { },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                Content =
                {
                    {
                        "Document",
                        new AttributeDef
                        {
                            Name = "Document",
                            NameLocations = { new RangePosition { Start = new Position { Line = 1, Character = 4 }, End = new Position { Line = 1, Character = 12 } } },
                            TypeDef = new RoleType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "DocumentClass" } } } },
                            }
                        }
                    },
                    {
                        "Action",
                        new AttributeDef
                        {
                            Name = "Action",
                            NameLocations = { new RangePosition { Start = new Position { Line = 2, Character = 4 }, End = new Position { Line = 2, Character = 10 } } },
                            TypeDef = new RoleType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "ActionClass" } } } },
                            }
                        }
                    },
                }
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitAssociationDef(p.associationDef()));
}
