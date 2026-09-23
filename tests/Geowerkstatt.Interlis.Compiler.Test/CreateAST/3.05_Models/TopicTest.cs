using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class TopicTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        // Cases with multiple topics or model level definitions cannot be parsed with the topicDef rule, they are only used for the compiler comparison.
        yield return FullFile(new(
            "Multiple topics in model",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                END TopicA;
                TOPIC TopicB =
                END TopicB;
            END Model.
            """,
            RefHB: "3.5.2",
            AssertOutput: false));

        yield return FullFile(new(
            "Inherited topic definitions are visible in extending topics",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC BaseTopic (ABSTRACT) =
                    CLASS Unit (ABSTRACT) =
                        Name : TEXT*20;
                    END Unit;
                END BaseTopic;
                TOPIC Countries EXTENDS BaseTopic =
                    CLASS Country EXTENDS Unit =
                    END Country;
                END Countries;
                TOPIC Cities EXTENDS Countries =
                    CLASS City EXTENDS Unit =
                    END City;
                END Cities;
            END Model.
            """,
            Description: """
                Extending a topic adds all names of the base topic to the extending topic's namespaces, so definitions
                inherited from the base chain (transitively) can be referenced by their unqualified name. This is the
                CHBase pattern (e.g. AdministrativeUnits_V2: CLASS Country EXTENDS AdministrativeUnit inside TOPIC
                Countries EXTENDS AdministrativeUnits).
                """,
            RefHB: "3.5.4-11",
            AssertOutput: false));

        yield return FullFile(new(
            "Topic extension across models with an EXTENDED class",
            """
            INTERLIS 2.4;
            MODEL Base AT "http://example.com" VERSION "1.0.0" =
                TOPIC Dictionaries (ABSTRACT) =
                    STRUCTURE Entry (ABSTRACT) =
                        Text : MANDATORY TEXT*50;
                    END Entry;
                    CLASS Dictionary =
                        Language : MANDATORY TEXT*2;
                        Entries : LIST OF Entry;
                    END Dictionary;
                END Dictionaries;
            END Base.
            MODEL Ext AT "http://example.com" VERSION "1.0.0" =
                IMPORTS Base;
                TOPIC Dictionaries (ABSTRACT) EXTENDS Base.Dictionaries =
                    STRUCTURE CountryName EXTENDS Entry =
                        Code : TEXT*3;
                    END CountryName;
                    CLASS Dictionary (EXTENDED) =
                    MANDATORY CONSTRAINT
                        Language == "de" OR Language == "fr";
                    END Dictionary;
                END Dictionaries;
            END Ext.
            """,
            Description: """
                The base topic may live in another model; an EXTENDED class element extends the same-named inherited
                class without naming it, so members of that inherited class stay reachable (the CHBase DictionariesCH_V2
                pattern: CLASS Dictionary (EXTENDED) with a constraint on the inherited Language attribute).
                """,
            RefHB: "3.5.4-11",
            AssertOutput: false));

        yield return FullFile(new(
            "Base model definitions resolve regardless of model order",
            """
            INTERLIS 2.4;
            MODEL First (en) AT "http://example.com" VERSION "1" =
                IMPORTS Second;
                DOMAIN
                    Kind = 0 .. 9;
                TOPIC Signs EXTENDS Second.Signs =
                    CLASS TextSign (EXTENDED) =
                        attr : Kind;
                    END TextSign;
                END Signs;
            END First.
            MODEL Second (en) AT "http://example.com" VERSION "1" =
                TOPIC Signs (ABSTRACT) =
                    CLASS TextSign (ABSTRACT) EXTENDS INTERLIS.METAOBJECT =
                    END TextSign;
                END Signs;
            END Second.
            """,
            Description: """
                Cross-model resolution must not depend on the model visit order: resolving 'Kind' inside the extending
                topic walks the base topic's classes and demands their EXTENDS references on demand — BEFORE the base
                model's own visit resolved its imports. The resolver anchors all import references up front, so the
                premature demand still finds INTERLIS.METAOBJECT (this is the CHBase/StandardSymbology pattern).
                """,
            RefHB: "3.5.4-11",
            Ili2cDivergenceReason: "ili2c reads a multi-model file in a single pass and rejects the import of a model defined later in the same file; we resolve models order-independently (the cross-file dependency closure requires it anyway), so we accept.",
            AssertOutput: false));

        yield return FullFile(new(
            "EXTENDED class without an inherited namesake is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS A (EXTENDED) =
                    END A;
                END Topic;
            END Model.
            """,
            Description: """
                EXTENDED marks the deliberate reuse of an inherited name; without a same-named element in the base topic
                chain there is nothing to extend. ili2c rejects this too ("EXTENDED does not make sense").
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.A' at 4:8-5:14: is marked EXTENDED but there is no inherited element of the same name."],
            RefHB: "3.5.4-11",
            AssertOutput: false));

        yield return Rule(new(
            "Topic definition",
            """
            TOPIC Test =
            END Test;
            """,
            RefHB: "3.5.2-1",
            Expected: new TopicDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 10),
                    new RangePosition(1, 4, 1, 8)
                },
            }));

        yield return Rule(new(
            "Abstract topic",
            """
            TOPIC Topic (ABSTRACT) =
            END Topic;
            """,
            RefHB: "3.5.2-1",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                Properties = { Property.Abstract },
            }));

        yield return Rule(new(
            "Topic with invalid property EXTENDED",
            """
            TOPIC Topic (EXTENDED) =
            END Topic;
            """,
            ExpectedLog: ["Compile error at 1:13-1:21 Property 'EXTENDED' is not one of the allowed properties ('ABSTRACT', 'FINAL')."],
            RefHB: "3.5.2-1",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                Properties = { Property.Extended },
            }));

        yield return Rule(new(
            "Final topic",
            """
            TOPIC Topic (FINAL) =
            END Topic;
            """,
            RefHB: "3.5.2-1",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                Properties = { Property.Final },
            }));

        yield return Rule(new(
            "Non abstract topic with abstract class",
            """
            TOPIC Topic =
                CLASS ClassName (ABSTRACT) =
                END ClassName;
            END Topic;
            """,
            RefHB: "3.5.2-1",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(3, 4, 3, 9)
                },
                Content =
                {
                    {
                        "ClassName",
                        new ClassDef
                        {
                            Name = "ClassName",
                            NameLocations =
                            {
                                new RangePosition(1, 10, 1, 19),
                                new RangePosition(2, 8, 2, 17)
                            },
                            Properties = { Property.Abstract },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic definition complete",
            """
            /** Doc-Comment */
            !!@ key=value
            VIEW TOPIC Test_A (ABSTRACT, FINAL) EXTENDS Test_B =
                BASKET OID AS oidDomain;
                OID AS INTERLIS.UUIDOID;
                DEPENDS ON Test_C, Test_D;
                DEFERRED GENERICS genericA, genericB;
            END Test_A;
            """,
            RefHB: "3.5.2-7",
            Ech0117: "5-5",
            Expected: new TopicDef
            {
                Name = "Test_A",
                NameLocations =
                {
                    new RangePosition(2, 11, 2, 17),
                    new RangePosition(7, 4, 7, 10)
                },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                IsView = true,
                Extends = new Reference<TopicDef> { Path = { new("Test_B") } },
                BasketOidType = new Reference<DomainDef> { Path = { new("oidDomain") } },
                OidType = new Reference<DomainDef> { Path = { new("INTERLIS"), new("UUIDOID") } },
                DependsOn =
                {
                    new Reference<TopicDef> { Path = { new("Test_C") } },
                    new Reference<TopicDef> { Path = { new("Test_D") } },
                },
                DeferredGenerics =
                {
                    new Reference<DomainDef> { Path = { new("genericA") } },
                    new Reference<DomainDef> { Path = { new("genericB") } },
                },
                Properties = { Property.Abstract, Property.Final }
            }));

        yield return Rule(new(
            "Topic extends nonexistent topic",
            """
            TOPIC Topic EXTENDS BaseTopic =
            END Topic;
            """,
            RefHB: "3.5.2-7",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                Extends = new Reference<TopicDef> { Path = { new("BaseTopic") } },
            }));

        yield return Rule(new(
            "Topic extends qualified topic reference",
            """
            TOPIC Topic EXTENDS BaseModel.BaseTopic =
            END Topic;
            """,
            RefHB: "3.5.2-9",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                Extends = new Reference<TopicDef> { Path = { new("BaseModel"), new("BaseTopic") } },
            }));

        yield return Rule(new(
            "Topic with multiple DEPENDS ON statements",
            """
            TOPIC TopicC =
                DEPENDS ON TopicA;
                DEPENDS ON TopicB;
            END TopicC;
            """,
            RefHB: "3.5.2-7",
            Expected: new TopicDef
            {
                Name = "TopicC",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 12),
                    new RangePosition(3, 4, 3, 10)
                },
                DependsOn =
                {
                    new Reference<TopicDef> { Path = { new("TopicA") } },
                    new Reference<TopicDef> { Path = { new("TopicB") } },
                },
            }));

        yield return Rule(new(
            "View topic with class",
            """
            VIEW TOPIC Topic =
                CLASS ClassName =
                END ClassName;
            END Topic;
            """,
            RefHB: "3.5.2-7",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 11, 0, 16),
                    new RangePosition(3, 4, 3, 9)
                },
                IsView = true,
                Content =
                {
                    {
                        "ClassName",
                        new ClassDef
                        {
                            Name = "ClassName",
                            NameLocations =
                            {
                                new RangePosition(1, 10, 1, 19),
                                new RangePosition(2, 8, 2, 17)
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "View topic with domain",
            """
            VIEW TOPIC Topic =
                DOMAIN
                    text = TEXT*20;
            END Topic;
            """,
            RefHB: "3.5.2-7",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 11, 0, 16),
                    new RangePosition(3, 4, 3, 9)
                },
                IsView = true,
                Content =
                {
                    {
                        "text",
                        new DomainDef
                        {
                            Name = "text",
                            NameLocations = { new RangePosition(2, 8, 2, 12) },
                            TypeDef = new TextType
                            {
                                Length = 20,
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                SourceRange = new RangePosition(2, 15, 2, 22),
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "View topic with unit",
            """
            VIEW TOPIC Topic =
                UNIT HorsePower [hp];
            END Topic;
            """,
            RefHB: "3.5.2-7",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 11, 0, 16),
                    new RangePosition(2, 4, 2, 9)
                },
                IsView = true,
                Content =
                {
                    {
                        "hp",
                        new UnitDef
                        {
                            Name = "hp",
                            Term = "HorsePower",
                            NameLocations = { new RangePosition(1, 21, 1, 23) },
                        }
                    }
                },
            }));

        yield return FullFile(new(
            "Topic extends final topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC A (FINAL) =
                END A;
                TOPIC B EXTENDS A =
                END B;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.B' at 5:4-6:10: can not extend 'Model.A' because it is declared FINAL."],
            RefHB: "3.5.2-7",
            AssertOutput: false));

        yield return FullFile(new(
            "Topic extends",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC BaseTopic (ABSTRACT) =
                END BaseTopic;
                TOPIC Topic EXTENDS BaseTopic =
                END Topic;
            END Model.
            """,
            RefHB: "3.5.2-7",
            AssertOutput: false));

        yield return Rule(new(
            "Topic with mismatched end name",
            """
            TOPIC Topic =
            END WrongName;
            """,
            ExpectedLog: ["Compile error at 2:4-2:13 Start name 'Topic' and end name 'WrongName' do not match."],
            RefHB: "3.5.2-7",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 13)
                },
            }));

        yield return Rule(new(
            "Topic with class",
            """
            TOPIC Topic =
                CLASS ClassName =
                END ClassName;
            END Topic;
            """,
            RefHB: "3.5.2-8",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(3, 4, 3, 9)
                },
                Content =
                {
                    {
                        "ClassName",
                        new ClassDef
                        {
                            Name = "ClassName",
                            NameLocations =
                            {
                                new RangePosition(1, 10, 1, 19),
                                new RangePosition(2, 8, 2, 17)
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic with structure",
            """
            TOPIC Topic =
                STRUCTURE StructName =
                END StructName;
            END Topic;
            """,
            RefHB: "3.5.2-8",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(3, 4, 3, 9)
                },
                Content =
                {
                    {
                        "StructName",
                        new ClassDef
                        {
                            Name = "StructName",
                            NameLocations =
                            {
                                new RangePosition(1, 14, 1, 24),
                                new RangePosition(2, 8, 2, 18)
                            },
                            IsStructure = true,
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic with function",
            """
            TOPIC Topic =
                FUNCTION Func(arg : TEXT) : TEXT;
            END Topic;
            """,
            RefHB: "3.5.2-8",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(2, 4, 2, 9)
                },
                Content =
                {
                    {
                        "Func",
                        new FunctionDef
                        {
                            Name = "Func",
                            NameLocations = { new RangePosition(1, 13, 1, 17) },
                            ReturnType = new TextType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                SourceRange = new RangePosition(1, 32, 1, 36),
                            },
                            Arguments =
                            {
                                new FunctionArgument
                                {
                                    Name = "arg",
                                    Type = new TextType
                                    {
                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                        SourceRange = new RangePosition(1, 24, 1, 28),
                                    },
                                },
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic with association",
            """
            TOPIC Topic =
                CLASS A =
                END A;
                ASSOCIATION Assoc =
                    roleA -- {0..*} A;
                    roleB -- {0..*} A;
                END Assoc;
            END Topic;
            """,
            RefHB: "3.5.2-8",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(7, 4, 7, 9)
                },
                Content =
                {
                    {
                        "A",
                        new ClassDef
                        {
                            Name = "A",
                            NameLocations =
                            {
                                new RangePosition(1, 10, 1, 11),
                                new RangePosition(2, 8, 2, 9)
                            },
                        }
                    },
                    {
                        "Assoc",
                        new AssociationDef
                        {
                            Name = "Assoc",
                            NameLocations =
                            {
                                new RangePosition(3, 16, 3, 21),
                                new RangePosition(6, 8, 6, 13)
                            },
                            Content =
                            {
                                {
                                    "roleA",
                                    new AttributeDef
                                    {
                                        Name = "roleA",
                                        NameLocations = { new RangePosition(4, 8, 4, 13) },
                                        TypeDef = new RoleType
                                        {
                                            Targets =
                                            {
                                                new RestrictedRef
                                                {
                                                    Value = new Reference<IInterlisDefinition> { Path = { new("A") } },
                                                },
                                            },
                                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                        },
                                    }
                                },
                                {
                                    "roleB",
                                    new AttributeDef
                                    {
                                        Name = "roleB",
                                        NameLocations = { new RangePosition(5, 8, 5, 13) },
                                        TypeDef = new RoleType
                                        {
                                            Targets =
                                            {
                                                new RestrictedRef
                                                {
                                                    Value = new Reference<IInterlisDefinition> { Path = { new("A") } },
                                                },
                                            },
                                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                        },
                                    }
                                },
                            },
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic with metadata basket",
            """
            TOPIC Topic =
                REFSYSTEM BASKET RefBasket ~ INTERLIS.REFSYSTEM;
            END Topic;
            """,
            RefHB: "3.5.2-8",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(2, 4, 2, 9)
                },
                Content =
                {
                    {
                        "RefBasket",
                        new MetaDataBasketDef
                        {
                            Name = "RefBasket",
                            NameLocations = { new RangePosition(1, 21, 1, 30) },
                            Kind = MetaDataBasketDef.BasketKind.Refsystem,
                            Topic = new Reference<TopicDef> { Path = { new("INTERLIS"), new("REFSYSTEM") } },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic with constraints",
            """
            TOPIC Topic =
                CLASS A =
                END A;
                CONSTRAINTS OF A =
                END;
            END Topic;
            """,
            RefHB: "3.5.2-8",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(5, 4, 5, 9)
                },
                Content =
                {
                    {
                        "A",
                        new ClassDef
                        {
                            Name = "A",
                            NameLocations =
                            {
                                new RangePosition(1, 10, 1, 11),
                                new RangePosition(2, 8, 2, 9)
                            },
                        }
                    },
                    {
                        "CONSTRAINTS OF A #1",
                        new ConstraintsBlockDef
                        {
                            Name = "CONSTRAINTS OF A #1",
                            Target = new Reference<IInterlisDefinition> { Path = { new("A") } },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic with view",
            """
            TOPIC Topic =
                CLASS A =
                END A;
                VIEW ViewName
                    PROJECTION OF A;
                =
                END ViewName;
            END Topic;
            """,
            RefHB: "3.5.2-8",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(7, 4, 7, 9)
                },
                Content =
                {
                    {
                        "A",
                        new ClassDef
                        {
                            Name = "A",
                            NameLocations =
                            {
                                new RangePosition(1, 10, 1, 11),
                                new RangePosition(2, 8, 2, 9)
                            },
                        }
                    },
                    {
                        "ViewName",
                        new ViewDef
                        {
                            Name = "ViewName",
                            NameLocations =
                            {
                                new RangePosition(3, 9, 3, 17),
                                new RangePosition(6, 8, 6, 16)
                            },
                            Content =
                            {
                                { "A", new BaseView { Name = "A", NameLocations = { new RangePosition(4, 22, 4, 23) }, Viewable = new Reference<IInterlisDefinition> { Path = { new("A") } } } },
                            },
                            Formation = new ProjectionView
                            {
                                Source = new BaseView { Name = "A", NameLocations = { new RangePosition(4, 22, 4, 23) }, Viewable = new Reference<IInterlisDefinition> { Path = { new("A") } } },
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic with graphic",
            """
            TOPIC Topic =
                CLASS A =
                END A;
                GRAPHIC GraphicName BASED ON A =
                END GraphicName;
            END Topic;
            """,
            RefHB: "3.5.2-8",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(5, 4, 5, 9)
                },
                Content =
                {
                    {
                        "A",
                        new ClassDef
                        {
                            Name = "A",
                            NameLocations =
                            {
                                new RangePosition(1, 10, 1, 11),
                                new RangePosition(2, 8, 2, 9)
                            },
                        }
                    },
                    {
                        "GraphicName",
                        new GraphicDef
                        {
                            Name = "GraphicName",
                            NameLocations =
                            {
                                new RangePosition(3, 12, 3, 23),
                                new RangePosition(4, 8, 4, 19)
                            },
                            BasedOn = new Reference<IInterlisDefinition> { Path = { new("A") } },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Topic with BASKET OID",
            """
            TOPIC Topic =
                BASKET OID AS INTERLIS.UUIDOID;
            END Topic;
            """,
            RefHB: "3.5.2-16",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(2, 4, 2, 9)
                },
                BasketOidType = new Reference<DomainDef> { Path = { new("INTERLIS"), new("UUIDOID") } },
            }));

        yield return Rule(new(
            "Topic with OID",
            """
            TOPIC Topic =
                OID AS INTERLIS.STANDARDOID;
            END Topic;
            """,
            RefHB: "3.5.2-16",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(2, 4, 2, 9)
                },
                OidType = new Reference<DomainDef> { Path = { new("INTERLIS"), new("STANDARDOID") } },
            }));

        yield return Rule(new(
            "Invalid OID definition",
            """
            TOPIC Topic =
                OID AS NO OID;
            END Topic;
            """,
            ExpectedLog: ["Compile error at 2:11-2:13 mismatched input 'NO' expecting {'HALIGNMENT', 'INTERLIS', 'METAOBJECT', 'NAME', 'REFSYSTEM', 'SIGN', 'URI', 'VALIGNMENT', IDENTIFIER}."],
            RefHB: "3.5.2-16",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(2, 4, 2, 9)
                },
                OidType = new Reference<DomainDef> { },
            }));

        yield return Rule(new(
            "Invalid basket OID definition",
            """
            TOPIC Topic =
                BASKET OID AS NO OID;
            END Topic;
            """,
            ExpectedLog: ["Compile error at 2:18-2:20 mismatched input 'NO' expecting {'HALIGNMENT', 'INTERLIS', 'METAOBJECT', 'NAME', 'REFSYSTEM', 'SIGN', 'URI', 'VALIGNMENT', IDENTIFIER}."],
            RefHB: "3.5.2-16",
            Expected: new TopicDef
            {
                Name = "Topic",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(2, 4, 2, 9)
                },
                BasketOidType = new Reference<DomainDef> { },
            }));

        yield return FullFile(new(
            "Topic redefines OID",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    BASKET OID AS INTERLIS.UUIDOID;
                END TopicA;
                TOPIC TopicB EXTENDS TopicA =
                    BASKET OID AS INTERLIS.I32OID;
                END TopicB;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.TopicB' at 6:4-8:15: the inherited BASKET OID definition 'UUIDOID' is concrete and can not be changed."],
            Ili2cDivergenceReason: """
                ili2c does not check topic OID assignments; RefHB 3.5.2-16 states an inherited assignment can not
                be changed (only repeated or refined along the RefHB 3.5.3-2 / 3.8.9-13 ladder), so we reject.
                """,
            RefHB: "3.5.2-16",
            AssertOutput: false));

        yield return Rule(new(
            "Topic which depends on inexistent topic",
            """
            TOPIC TopicB =
                DEPENDS ON TopicA;
            END TopicB;
            """,
            RefHB: "3.5.2-17",
            Expected: new TopicDef
            {
                Name = "TopicB",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 12),
                    new RangePosition(2, 4, 2, 10)
                },
                DependsOn =
                {
                    new Reference<TopicDef> { Path = { new("TopicA") } },
                },
            }));

        yield return FullFile(new(
            "Topic with DEPENDS ON",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                END TopicA;
                TOPIC TopicB =
                    DEPENDS ON TopicA;
                END TopicB;
            END Model.
            """,
            RefHB: "3.5.2-17",
            AssertOutput: false));

        yield return FullFile(new(
            "Missing depends on declaration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    CLASS Document =
                    END Document;
                END TopicA;
                TOPIC TopicB =
                    CLASS Object =
                    END Object;
                    ASSOCIATION =
                        Document (EXTERNAL) -- Model.TopicA.Document;
                        Object -- Object;
                    END;
                END TopicB;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.TopicB.DocumentObject -> Document' at 11:12-11:57: the cross-topic role requires a topic dependency on 'TopicA'."],
            RefHB: "3.5.2-17",
            AssertOutput: false));

        yield return FullFile(new(
            "Cyclic dependency",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    DEPENDS ON TopicB;
                END TopicA;
                TOPIC TopicB =
                    DEPENDS ON TopicA;
                END TopicB;
            END Model.
            """,
            RefHB: "3.5.2-17",
            AssertOutput: false,
            Ili2cDivergenceReason: "we accept a cyclic/forward DEPENDS ON, ili2c rejects it (it requires the depended-on topic to be declared earlier)"));

        yield return FullFile(new(
            "Missing deferred generics declaration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Coord (GENERIC) = COORD NUMERIC, NUMERIC;

                    Coord_LV95 EXTENDS Coord = COORD
                        2460000.000 .. 2870000.000,
                        1045000.000 .. 1310000.000,
                        ROTATION 2 -> 1 REFSYS "EPSG:2056";
                    Coord_LV03 EXTENDS Coord = COORD
                        460000.000 .. 870000.000,
                        45000.000 .. 310000.000,
                        ROTATION 2 -> 1 REFSYS "EPSG:21781";

                CONTEXT default =
                    Coord = Coord_LV03 OR Coord_LV95;

                TOPIC TestA =
                    CLASS ClassA =
                        attr : Coord;
                    END ClassA;
                END TestA;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.TestA' at 18:4-22:14: must declare DEFERRED GENERICS for the generic domain 'Coord'."],
            RefHB: "3.5.2-18",
            AssertOutput: false));

        yield return FullFile(new(
            "Deferred generics declaration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Coord (GENERIC) = COORD NUMERIC, NUMERIC;

                    Coord_LV95 EXTENDS Coord = COORD
                        2460000.000 .. 2870000.000,
                        1045000.000 .. 1310000.000,
                        ROTATION 2 -> 1 REFSYS "EPSG:2056";
                    Coord_LV03 EXTENDS Coord = COORD
                        460000.000 .. 870000.000,
                        45000.000 .. 310000.000,
                        ROTATION 2 -> 1 REFSYS "EPSG:21781";

                CONTEXT default =
                    Coord = Coord_LV03 OR Coord_LV95;

                TOPIC TestA =
                    DEFERRED GENERICS Coord;
                    CLASS ClassA =
                        attr : Coord;
                    END ClassA;
                END TestA;
            END Model.
            """,
            ExpectedLog: [],
            RefHB: "3.5.2-18",
            AssertOutput: false));

        yield return FullFile(new(
            "Topics forming an EXTENDS cycle",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC A EXTENDS B =
                END A;
                TOPIC B EXTENDS A =
                END B;
            END Model.
            """,
            Description: """
                A circular EXTENDS chain makes each topic in the cycle transitively its own base; ili2c rejects the
                cycle too (err_cyclicExtension).
                """,
            RefHB: "3.5.2-4",
            ExpectedLog:
            [
                "Type check error in 'Model.A' at 3:4-4:10: the topic transitively EXTENDS itself.",
                "Type check error in 'Model.B' at 5:4-6:10: the topic transitively EXTENDS itself.",
            ],
            AssertOutput: false));


        yield return FullFile(new(
            "Topic extension keeps or refines the inherited OID assignments",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Base =
                    OID AS INTERLIS.STANDARDOID;
                END Base;
                TOPIC Extension EXTENDS Base =
                    BASKET OID AS INTERLIS.UUIDOID;
                    OID AS INTERLIS.STANDARDOID;
                END Extension;
                TOPIC AnyBase (ABSTRACT) =
                    BASKET OID AS INTERLIS.ANYOID;
                END AnyBase;
                TOPIC Concrete EXTENDS AnyBase =
                    BASKET OID AS INTERLIS.UUIDOID;
                END Concrete;
            END Model.
            """,
            Description: """
                Repeating the inherited OID assignment unchanged, adding an assignment the base chain never made,
                and concretizing an inherited ANYOID assignment are all fine: RefHB 3.5.2-16 forbids changing an
                inherited assignment, read in harmony with the RefHB 3.5.3-2 / 3.8.9-13 replacement ladder — an
                ANYOID assignment is explicitly "noch offen" (RefHB 3.8.9-14), and for the BASKET OID no
                class-level concretization exists, so the topic extension is the only place to close it.
                """,
            RefHB: "3.5.2-16",
            AssertOutput: false));

        yield return FullFile(new(
            "Topic extension changes the inherited OID assignments",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Base =
                    OID AS INTERLIS.STANDARDOID;
                END Base;
                TOPIC Extension EXTENDS Base =
                    OID AS INTERLIS.I32OID;
                END Extension;
            END Model.
            """,
            RefHB: "3.5.2-16",
            ExpectedLog: ["Type check error in 'Model.Extension' at 6:4-8:18: the inherited OID definition 'STANDARDOID' is concrete and can not be changed."],
            Ili2cDivergenceReason: """
                ili2c does not check topic OID assignments; RefHB 3.5.2-16 states an inherited assignment can not
                be changed (only repeated or refined along the RefHB 3.5.3-2 / 3.8.9-13 ladder), so we reject.
                """,
            AssertOutput: false));


        yield return FullFile(new(
            "OID definitions referencing a non-OID domain",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    NotOid = TEXT*8;
                TOPIC Topic =
                    BASKET OID AS NotOid;
                    OID AS NotOid;
                    CLASS A =
                        OID AS NotOid;
                    END A;
                END Topic;
            END Model.
            """,
            Description: """
                An OID assignment names a value range for object identifications, so the referenced domain must be
                an OID value range (RefHB 3.8.9-1/-3) — on topics, baskets and classes alike (ili2c agrees:
                "Domain ... should be an OIDType").
                """,
            RefHB: "3.8.9-1",
            ExpectedLog:
            [
                "Type check error in 'Model.Topic' at 5:4-11:14: the OID definition 'NotOid' must be an OID domain.",
                "Type check error in 'Model.Topic' at 5:4-11:14: the BASKET OID definition 'NotOid' must be an OID domain.",
                "Type check error in 'Model.Topic.A' at 8:8-10:14: the OID definition 'NotOid' must be an OID domain.",
            ],
            AssertOutput: false));


        yield return FullFile(new(
            "Topic without OID definitions extended with concrete ones",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Silent =
                END Silent;
                TOPIC Extension EXTENDS Silent =
                    BASKET OID AS INTERLIS.STANDARDOID;
                    OID AS INTERLIS.UUIDOID;
                END Extension;
            END Model.
            """,
            Description: """
                RefHB 3.5.2-16 only locks an assignment the base chain made ("Wurde einem Thema ein
                OID-Wertebereich zugeordnet ..."); a topic whose chain is silent on both slots leaves the
                extension free to introduce concrete definitions.
                """,
            RefHB: "3.5.2-16",
            AssertOutput: false));


        yield return FullFile(new(
            "Concrete topic with open OID definitions",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    BASKET OID AS INTERLIS.ANYOID;
                    OID AS INTERLIS.ANYOID;
                END Topic;
            END Model.
            """,
            Description: """
                An ANYOID assignment declares that identifications are expected while their exact definition is
                still open — only an abstract topic can leave that undecided (RefHB 3.8.9-14).
                """,
            RefHB: "3.8.9-14",
            ExpectedLog:
            [
                "Type check error in 'Model.Topic' at 3:4-6:14: must be declared ABSTRACT because its OID definition 'ANYOID' is still open.",
                "Type check error in 'Model.Topic' at 3:4-6:14: must be declared ABSTRACT because its BASKET OID definition 'ANYOID' is still open.",
            ],
            Ili2cDivergenceReason: """
                ili2c accepts ANYOID assignments on concrete classes and topics; RefHB 3.8.9-14 reserves ANYOID
                for abstract topics and classes (otherwise it is only usable as an attribute value range), so we
                reject.
                """,
            AssertOutput: false));

    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
        {fragment}
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(TopicTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadTopicDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitTopicDef(p.topicDef()));
    }
}
