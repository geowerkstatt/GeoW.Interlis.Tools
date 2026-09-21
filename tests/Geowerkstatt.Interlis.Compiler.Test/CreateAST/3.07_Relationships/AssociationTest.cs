using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class AssociationTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Association definition",
            """
            ASSOCIATION Test =
            END Test;
            """,
            RefHB: "3.7.1-1",
            Expected: new AssociationDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(0, 12, 0, 16),
                    new RangePosition(1, 4, 1, 8),
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            },
            Ili2cDivergenceReason: "we accept an association without roles, ili2c rejects it"));

        yield return Rule(new(
            "Association definition complete",
            """
            ASSOCIATION Test_A (ABSTRACT, EXTENDED) EXTENDS Test_B DERIVED FROM Base ~ Test_C =
                OID AS oidType;
                ATTRIBUTE
                CARDINALITY = {5 .. 42} ;
            END Test_A;
            """,
            RefHB: "3.7.1-1",
            Expected: new AssociationDef
            {
                Name = "Test_A",
                NameLocations =
                {
                    new RangePosition(0, 12, 0, 18),
                    new RangePosition(4, 4, 4, 10),
                },
                Cardinality = new Cardinality { Min = 5, Max = 42 },
                Extends = new Reference<AssociationDef> { Path = { "Test_B" }, SourceRange = new RangePosition(0, 48, 0, 54) },
                DerivedFrom = new BaseView { Name = "Base", NameLocations = { new RangePosition(0, 68, 0, 72) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Test_C" }, SourceRange = new RangePosition(0, 75, 0, 81) } },
                // The DERIVED FROM base is also registered in Content under its base name (same instance as DerivedFrom).
                Content =
                {
                    { "Base", new BaseView { Name = "Base", NameLocations = { new RangePosition(0, 68, 0, 72) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Test_C" }, SourceRange = new RangePosition(0, 75, 0, 81) } } },
                },
                OidType = new Reference<DomainDef> { Path = { "oidType" }, SourceRange = new RangePosition(1, 11, 1, 18) },
                Properties = { Property.Abstract, Property.Extended },
            }));

        yield return Rule(new(
            "Association without name",
            """
            ASSOCIATION =
                Document -- DocumentClass;
                Action -- ActionClass;
            END;
            """,
            RefHB: "3.7.1-1",
            Expected: new AssociationDef
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
                                Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "DocumentClass" }, SourceRange = new RangePosition(1, 16, 1, 29) } } },
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
                                Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "ActionClass" }, SourceRange = new RangePosition(2, 14, 2, 25) } } },
                            }
                        }
                    },
                }
            }));

        yield return Rule(new(
            "Association with two roles and alternative target",
            """
            ASSOCIATION A =
            K -- K;
            KL -- K OR L;
            END A;
            """,
            RefHB: "3.7.1-3",
            Expected: new AssociationDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 12, 0, 13), new RangePosition(3, 4, 3, 5) },
                Content =
                {
                    {
                        "K",
                        new AttributeDef
                        {
                            Name = "K",
                            NameLocations = { new RangePosition(1, 0, 1, 1) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "K" }, SourceRange = new RangePosition(1, 5, 1, 6) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                    {
                        "KL",
                        new AttributeDef
                        {
                            Name = "KL",
                            NameLocations = { new RangePosition(2, 0, 2, 2) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "K" }, SourceRange = new RangePosition(2, 6, 2, 7) } },
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "L" }, SourceRange = new RangePosition(2, 11, 2, 12) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Extended association role with restriction",
            """
            ASSOCIATION A1 EXTENDS A =
            KL (EXTENDED) -- L RESTRICTION (L1);
            END A1;
            """,
            RefHB: "3.7.1-7",
            Expected: new AssociationDef
            {
                Name = "A1",
                NameLocations = { new RangePosition(0, 12, 0, 14), new RangePosition(2, 4, 2, 6) },
                Extends = new Reference<AssociationDef> { Path = { "A" }, SourceRange = new RangePosition(0, 23, 0, 24) },
                Content =
                {
                    {
                        "KL",
                        new AttributeDef
                        {
                            Name = "KL",
                            NameLocations = { new RangePosition(1, 0, 1, 2) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef
                                    {
                                        Value = new Reference<IInterlisDefinition> { Path = { "L" }, SourceRange = new RangePosition(1, 17, 1, 18) },
                                        Restrictions = { new Reference<IInterlisDefinition> { Path = { "L1" }, SourceRange = new RangePosition(1, 32, 1, 34) } },
                                    },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                            Properties = { Property.Extended },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Extended association role with hiding",
            """
            ASSOCIATION A1 EXTENDS A =
            K (EXTENDED, HIDING) -- K1;
            END A1;
            """,
            RefHB: "3.7.1-10",
            Expected: new AssociationDef
            {
                Name = "A1",
                NameLocations = { new RangePosition(0, 12, 0, 14), new RangePosition(2, 4, 2, 6) },
                Extends = new Reference<AssociationDef> { Path = { "A" }, SourceRange = new RangePosition(0, 23, 0, 24) },
                Content =
                {
                    {
                        "K",
                        new AttributeDef
                        {
                            Name = "K",
                            NameLocations = { new RangePosition(1, 0, 1, 1) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "K1" }, SourceRange = new RangePosition(1, 24, 1, 26) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                            Properties = { Property.Extended, Property.Hiding },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Association with NO OID",
            """
            ASSOCIATION Test =
            NO OID;
            role -- Target;
            other -- Target2;
            END Test;
            """,
            RefHB: "3.7.1-14",
            Expected: new AssociationDef
            {
                Name = "Test",
                NameLocations = { new RangePosition(0, 12, 0, 16), new RangePosition(4, 4, 4, 8) },
                Content =
                {
                    {
                        "role",
                        new AttributeDef
                        {
                            Name = "role",
                            NameLocations = { new RangePosition(2, 0, 2, 4) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target" }, SourceRange = new RangePosition(2, 8, 2, 14) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                    {
                        "other",
                        new AttributeDef
                        {
                            Name = "other",
                            NameLocations = { new RangePosition(3, 0, 3, 5) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target2" }, SourceRange = new RangePosition(3, 9, 3, 16) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "NOOID" } },
            }));

        yield return Rule(new(
            "Association extends qualified reference",
            """
            ASSOCIATION A1 EXTENDS Other.Sub.A =
            END A1;
            """,
            RefHB: "3.7.1-15",
            Expected: new AssociationDef
            {
                Name = "A1",
                NameLocations = { new RangePosition(0, 12, 0, 14), new RangePosition(1, 4, 1, 6) },
                Extends = new Reference<AssociationDef> { Path = { "Other", "Sub", "A" }, SourceRange = new RangePosition(0, 23, 0, 34) },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Role with cardinality",
            """
            ASSOCIATION A =
            role -- {1..5} Target;
            other -- Target2;
            END A;
            """,
            RefHB: "3.7.1-16",
            Expected: new AssociationDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 12, 0, 13), new RangePosition(3, 4, 3, 5) },
                Content =
                {
                    {
                        "role",
                        new AttributeDef
                        {
                            Name = "role",
                            NameLocations = { new RangePosition(1, 0, 1, 4) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target" }, SourceRange = new RangePosition(1, 15, 1, 21) } },
                                },
                                Cardinality = new Cardinality { Min = 1, Max = 5 },
                            },
                        }
                    },
                    {
                        "other",
                        new AttributeDef
                        {
                            Name = "other",
                            NameLocations = { new RangePosition(2, 0, 2, 5) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target2" }, SourceRange = new RangePosition(2, 9, 2, 16) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Role properties abstract and final",
            """
            ASSOCIATION A =
            role (ABSTRACT) -- Target;
            other (FINAL) -- Target2;
            END A;
            """,
            RefHB: "3.7.1-16",
            Expected: new AssociationDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 12, 0, 13), new RangePosition(3, 4, 3, 5) },
                Content =
                {
                    {
                        "role",
                        new AttributeDef
                        {
                            Name = "role",
                            NameLocations = { new RangePosition(1, 0, 1, 4) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target" }, SourceRange = new RangePosition(1, 19, 1, 25) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                            Properties = { Property.Abstract },
                        }
                    },
                    {
                        "other",
                        new AttributeDef
                        {
                            Name = "other",
                            NameLocations = { new RangePosition(2, 0, 2, 5) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target2" }, SourceRange = new RangePosition(2, 17, 2, 24) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                            Properties = { Property.Final },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Role with role factor",
            """
            ASSOCIATION A =
            role -- Target := thePath;
            other -- Target2;
            END A;
            """,
            RefHB: "3.7.1-16",
            Expected: new AssociationDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 12, 0, 13), new RangePosition(3, 4, 3, 5) },
                Content =
                {
                    {
                        "role",
                        new AttributeDef
                        {
                            Name = "role",
                            NameLocations = { new RangePosition(1, 0, 1, 4) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target" }, SourceRange = new RangePosition(1, 8, 1, 14) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                            Values =
                            {
                                new PathExpression
                                {
                                    Path = { new IdentifierPathElement { Value = "thePath" } },
                                },
                            },
                        }
                    },
                    {
                        "other",
                        new AttributeDef
                        {
                            Name = "other",
                            NameLocations = { new RangePosition(2, 0, 2, 5) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target2" }, SourceRange = new RangePosition(2, 9, 2, 16) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Aggregation role strength",
            """
            ASSOCIATION A =
            whole -<> Whole;
            part -- Part;
            END A;
            """,
            RefHB: "3.7.2-3",
            Expected: new AssociationDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 12, 0, 13), new RangePosition(3, 4, 3, 5) },
                Content =
                {
                    {
                        "whole",
                        new AttributeDef
                        {
                            Name = "whole",
                            NameLocations = { new RangePosition(1, 0, 1, 5) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Whole" }, SourceRange = new RangePosition(1, 10, 1, 15) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                Relationship = RoleType.RelationshipType.Aggregation,
                            },
                        }
                    },
                    {
                        "part",
                        new AttributeDef
                        {
                            Name = "part",
                            NameLocations = { new RangePosition(2, 0, 2, 4) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Part" }, SourceRange = new RangePosition(2, 8, 2, 12) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Composition role strength",
            """
            ASSOCIATION A =
            whole -<#> Whole;
            part -- Part;
            END A;
            """,
            RefHB: "3.7.2-4",
            Expected: new AssociationDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 12, 0, 13), new RangePosition(3, 4, 3, 5) },
                Content =
                {
                    {
                        "whole",
                        new AttributeDef
                        {
                            Name = "whole",
                            NameLocations = { new RangePosition(1, 0, 1, 5) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Whole" }, SourceRange = new RangePosition(1, 11, 1, 16) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Relationship = RoleType.RelationshipType.Composition,
                            },
                        }
                    },
                    {
                        "part",
                        new AttributeDef
                        {
                            Name = "part",
                            NameLocations = { new RangePosition(2, 0, 2, 4) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Part" }, SourceRange = new RangePosition(2, 8, 2, 12) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "Ordered role",
            """
            ASSOCIATION A =
            role (ORDERED) -- Target;
            other -- Target2;
            END A;
            """,
            RefHB: "3.7.4-1",
            Expected: new AssociationDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 12, 0, 13), new RangePosition(3, 4, 3, 5) },
                Content =
                {
                    {
                        "role",
                        new AttributeDef
                        {
                            Name = "role",
                            NameLocations = { new RangePosition(1, 0, 1, 4) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target" }, SourceRange = new RangePosition(1, 18, 1, 24) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound, Ordered = true },
                            },
                        }
                    },
                    {
                        "other",
                        new AttributeDef
                        {
                            Name = "other",
                            NameLocations = { new RangePosition(2, 0, 2, 5) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target2" }, SourceRange = new RangePosition(2, 9, 2, 16) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return Rule(new(
            "External role",
            """
            ASSOCIATION A =
            role (EXTERNAL) -- Target;
            other -- Target2;
            END A;
            """,
            RefHB: "3.7.5-1",
            Expected: new AssociationDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 12, 0, 13), new RangePosition(3, 4, 3, 5) },
                Content =
                {
                    {
                        "role",
                        new AttributeDef
                        {
                            Name = "role",
                            NameLocations = { new RangePosition(1, 0, 1, 4) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target" }, SourceRange = new RangePosition(1, 19, 1, 25) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                            Properties = { Property.External },
                        }
                    },
                    {
                        "other",
                        new AttributeDef
                        {
                            Name = "other",
                            NameLocations = { new RangePosition(2, 0, 2, 5) },
                            TypeDef = new RoleType
                            {
                                Targets =
                                {
                                    new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "Target2" }, SourceRange = new RangePosition(2, 9, 2, 16) } },
                                },
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            },
                        }
                    },
                },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            }));

        yield return FullFile(new(
            "Association role to ANYSTRUCTURE is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Target =
                    END Target;
                    ASSOCIATION Assoc =
                        role1 -- Target;
                        role2 -- ANYSTRUCTURE;
                    END Assoc;
                END Topic;
            END Model.
            """,
            Description: """
                An association role targets a class or association (RestrictedClassOrAssRef); ANYSTRUCTURE (a structure
                placeholder, RefHB 3.6.1-17) is not a valid role target.
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.Assoc -> role2' at 8:12-8:34: ANYSTRUCTURE is not allowed as an association role target."],
            RefHB: "3.7.1",
            AssertOutput: false));

        yield return FullFile(new(
            "Association extending itself",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS X =
                    END X;
                    ASSOCIATION a EXTENDS a =
                    END a;
                END Topic;
            END Model.
            """,
            Description: """
                A circular EXTENDS chain makes the association transitively its own base (ili2c rejects the model
                too: its single-pass name resolution can not even resolve the self-reference).
                """,
            RefHB: "3.7.1-7",
            ExpectedLog: ["Type check error in 'Model.Topic.a' at 6:8-7:14: the association transitively EXTENDS itself."],
            AssertOutput: false));


        yield return FullFile(new(
            "Extended role narrows cardinality and keeps the order",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS X =
                    END X;
                    CLASS Y =
                    END Y;
                    ASSOCIATION A1 =
                        rx -- {0..5} X;
                        ry -- Y;
                    END A1;
                    ASSOCIATION A2 EXTENDS A1 =
                        rx (EXTENDED, ORDERED) -- {1..4} X;
                    END A2;
                END Topic;
            END Model.
            """,
            Description: """
                A role extension may restrict the inherited target classes and cardinality (RefHB 3.7.1); shrinking
                the link population and declaring the link set ORDERED are both restrictions.
                """,
            RefHB: "3.7.1-7",
            AssertOutput: false));

        yield return FullFile(new(
            "Extended role drops the inherited order",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS X =
                    END X;
                    CLASS Y =
                    END Y;
                    ASSOCIATION A1 =
                        rx (ORDERED) -- X;
                        ry -- Y;
                    END A1;
                    ASSOCIATION A2 EXTENDS A1 =
                        rx (EXTENDED) -- X;
                    END A2;
                END Topic;
            END Model.
            """,
            RefHB: "3.7.4-1",
            ExpectedLog: ["Type check error in 'Model.Topic.A2 -> rx' at 13:12-13:31: an unordered role can not extend an ordered one."],
            AssertOutput: false));

        yield return FullFile(new(
            "Extended role widens the cardinality",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS X =
                    END X;
                    CLASS Y =
                    END Y;
                    ASSOCIATION A1 =
                        rx -- {0..5} X;
                        ry -- Y;
                    END A1;
                    ASSOCIATION A2 EXTENDS A1 =
                        rx (EXTENDED) -- {0..9} X;
                    END A2;
                END Topic;
            END Model.
            """,
            RefHB: "3.7.1-7",
            ExpectedLog: ["Type check error in 'Model.Topic.A2 -> rx' at 13:12-13:38: the cardinality must not be wider than the inherited cardinality."],
            AssertOutput: false));


        yield return FullFile(new(
            "Role targeting a NO OID class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS N =
                        NO OID;
                    END N;
                    CLASS Y =
                    END Y;
                    ASSOCIATION a =
                        rn -- N;
                        ry -- Y;
                    END a;
                END Topic;
            END Model.
            """,
            Description: """
                NO OID declares the class's object identification unstable, and as a consequence no relationships
                may be defined onto the class (RefHB 3.5.3-2).
                """,
            RefHB: "3.5.3-2",
            ExpectedLog: ["Type check error in 'Model.Topic.a -> rn' at 10:12-10:20: can not reference 'N' because it has no stable object identification (NO OID)."],
            Ili2cDivergenceReason: """
                ili2c accepts roles targeting classes declared with NO OID; RefHB 3.5.3-2 states such references
                can not be defined, so we reject.
                """,
            AssertOutput: false));

    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                {fragment}
            END Topic;
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(AssociationTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadAssociationDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitAssociationDef(p.associationDef()));
    }

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }
}
