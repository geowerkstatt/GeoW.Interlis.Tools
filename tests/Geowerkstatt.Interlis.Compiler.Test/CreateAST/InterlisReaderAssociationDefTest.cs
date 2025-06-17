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
                    new RangePosition(0, 12, 0, 16),
                    new RangePosition(1, 4, 1, 8),
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
                    new RangePosition(0, 12, 0, 18),
                    new RangePosition(4, 4, 4, 10),
                },
                Cardinality = new Cardinality { Min = 5, Max = 42 },
                Extends = new Reference<AssociationDef> { Path = { "Test_B" }, ReferenceLocation = new RangePosition(0, 48, 0, 54) },
                OidType = new Reference<DomainDef> { Path = { "oidType" }, ReferenceLocation = new RangePosition(1, 11, 1, 18) },
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
                            NameLocations = { new RangePosition(1, 4, 1, 12) },
                            TypeDef = new RoleType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "DocumentClass" }, ReferenceLocation = new RangePosition(1, 16, 1, 29) } } },
                            }
                        }
                    },
                    {
                        "Action",
                        new AttributeDef
                        {
                            Name = "Action",
                            NameLocations = { new RangePosition(2, 4, 2, 10) },
                            TypeDef = new RoleType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "ActionClass" }, ReferenceLocation = new RangePosition(2, 14, 2, 25) } } },
                            }
                        }
                    },
                }
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitAssociationDef(p.associationDef()));
}
