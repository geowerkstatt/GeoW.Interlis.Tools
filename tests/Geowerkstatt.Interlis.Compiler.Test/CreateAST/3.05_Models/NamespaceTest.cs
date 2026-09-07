using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class NamespaceTest
{
    // Name uniqueness within a namespace is enforced while building the AST (SetContentDictionary),
    // so duplicate-name cases can be tested with the modelDef rule. Visibility, qualification and
    // import resolution only happen later (full file), so those cases are compiler-comparison only.
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        // Visibility, qualification, shadowing and import resolution span multiple models or topics
        // and only surface during full-file resolution, so these cases are compiler-comparison only.
        yield return FullFile(new(
            "Duplicate IMPORTS of same model",
            """
            INTERLIS 2.4;
            MODEL ModelA AT "http://example.com" VERSION "1.0.0" =
            END ModelA.
            MODEL ModelB AT "http://example.com" VERSION "1.0.0" =
                IMPORTS ModelA;
                IMPORTS ModelA;
            END ModelB.
            """,
            ExpectedLog: ["Compile error at line 6:12 Duplicate import ModelA."],
            RefHB: "3.5.4",
            AssertOutput: false,
            Ili2cDivergenceReason: "RefHB-silent (3.5.1 does not forbid a duplicate IMPORTS): we keep the diagnostic, ili2c silently accepts"));

        yield return Rule(new(
            "Duplicate topic name in model",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                END TopicA;
                TOPIC TopicA =
                END TopicA;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 1:6 An element with name TopicA already exists in the scope Model."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(5, 4, 5, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "TopicA",
                        new TopicDef
                        {
                            Name = "TopicA",
                            NameLocations = { new RangePosition(1, 10, 1, 16), new RangePosition(2, 8, 2, 14) },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Duplicate class name in topic",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassName =
                    END ClassName;
                    CLASS ClassName =
                    END ClassName;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 2:10 An element with name ClassName already exists in the scope Topic."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(7, 4, 7, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "Topic",
                        new TopicDef
                        {
                            Name = "Topic",
                            NameLocations = { new RangePosition(1, 10, 1, 15), new RangePosition(6, 8, 6, 13) },
                            Content =
                            {
                                {
                                    "ClassName",
                                    new ClassDef
                                    {
                                        Name = "ClassName",
                                        NameLocations = { new RangePosition(2, 14, 2, 23), new RangePosition(3, 12, 3, 21) },
                                    }
                                }
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Duplicate attribute name in class",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassName =
                        Attr1 : TEXT*20;
                        Attr1 : TEXT*10;
                    END ClassName;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 3:14 An element with name Attr1 already exists in the scope ClassName."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(7, 4, 7, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "Topic",
                        new TopicDef
                        {
                            Name = "Topic",
                            NameLocations = { new RangePosition(1, 10, 1, 15), new RangePosition(6, 8, 6, 13) },
                            Content =
                            {
                                {
                                    "ClassName",
                                    new ClassDef
                                    {
                                        Name = "ClassName",
                                        NameLocations = { new RangePosition(2, 14, 2, 23), new RangePosition(5, 12, 5, 21) },
                                        Content =
                                        {
                                            {
                                                "Attr1",
                                                new AttributeDef
                                                {
                                                    Name = "Attr1",
                                                    NameLocations = { new RangePosition(3, 12, 3, 17) },
                                                    TypeDef = new TextType { Length = 20, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(3, 20, 3, 27) }
                                                }
                                            }
                                        },
                                    }
                                }
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Domain and topic with same name in model",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Topic = TEXT*20;
                TOPIC Topic =
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 1:6 An element with name Topic already exists in the scope Model."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(5, 4, 5, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "Topic",
                        new DomainDef
                        {
                            Name = "Topic",
                            NameLocations = { new RangePosition(2, 8, 2, 13) },
                            TypeDef = new TextType { Length = 20, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }, SourceRange = new RangePosition(2, 16, 2, 23) }
                        }
                    }
                },
            }));

        yield return FullFile(new(
            "Attribute and multiple constraint with same name",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        Attr: 0..10;
                        MANDATORY CONSTRAINT Attr: Attr <> 5;
                        MANDATORY CONSTRAINT Attr: Attr <> 7;
                    END Class;
                END Topic;
            END Model.
            """,
            Description: """
                An attribute and any number of constraints may share a name: constraint names are for messages
                only and live in a separate list (ClassDef.Constraints), so they never collide with the namespace
                (ClassDef.Content). Hence no diagnostics and all three "Attr" coexist.
                """,
            RefHB: "3.5.4-1",
            Expected: TestTools.Build(() =>
            {
                // Hoisted so the resolved object paths can point their Target at this exact attribute.
                var attr = new AttributeDef
                {
                    Name = "Attr",
                    TypeDef = new DecimalType
                    {
                        Min = 0,
                        Max = 10,
                        Precision = 0,
                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                    },
                };
                var classDef = new ClassDef
                {
                    Name = "Class",
                    Content = { { "Attr", attr } },
                    Constraints =
                    {
                        new MandatoryConstraint
                        {
                            Name = "Attr",
                            Condition = new ComparisonExpression
                            {
                                Operator = ComparisonExpression.ComparisonOperator.NotEqual,
                                FirstOperand = new PathExpression { Path = { new IdentifierPathElement { Value = "Attr" } }, Target = attr },
                                SecondOperand = new NumericConstant { Value = 5 },
                            },
                        },
                        new MandatoryConstraint
                        {
                            Name = "Attr",
                            Condition = new ComparisonExpression
                            {
                                Operator = ComparisonExpression.ComparisonOperator.NotEqual,
                                FirstOperand = new PathExpression { Path = { new IdentifierPathElement { Value = "Attr" } }, Target = attr },
                                SecondOperand = new NumericConstant { Value = 7 },
                            },
                        },
                    },
                };

                return new InterlisEnvironment
                {
                    Version = 2.4,
                    Content =
                    {
                        { InternalModel.Interlis.Name, InternalModel.Interlis },
                        {
                            "Model",
                            new ModelDef
                            {
                                Name = "Model",
                                URI = "http://example.com",
                                Version = "1.0.0",
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                                },
                                Content =
                                {
                                    {
                                        "Topic",
                                        new TopicDef
                                        {
                                            Name = "Topic",
                                            Content =
                                            {
                                                { "Class", classDef },
                                            },
                                        }
                                    },
                                }
                            }
                        },
                    },
                };
            })));

        yield return Rule(new(
            "Duplicate structure name in model",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE StructName =
                END StructName;
                STRUCTURE StructName =
                END StructName;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 1:6 An element with name StructName already exists in the scope Model."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(5, 4, 5, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "StructName",
                        new ClassDef
                        {
                            Name = "StructName",
                            NameLocations = { new RangePosition(1, 14, 1, 24), new RangePosition(2, 8, 2, 18) },
                            IsStructure = true,
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Duplicate domain name in model",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    D1 = TEXT*20;
                    D1 = TEXT*10;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 1:6 An element with name D1 already exists in the scope Model."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(4, 4, 4, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "D1",
                        new DomainDef
                        {
                            Name = "D1",
                            NameLocations = { new RangePosition(2, 8, 2, 10) },
                            TypeDef = new TextType { Length = 20, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }, SourceRange = new RangePosition(2, 13, 2, 20) }
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Duplicate domain name in topic",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    DOMAIN
                        LocalDom = TEXT*20;
                        LocalDom = TEXT*10;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 2:10 An element with name LocalDom already exists in the scope Topic."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(6, 4, 6, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "Topic",
                        new TopicDef
                        {
                            Name = "Topic",
                            NameLocations = { new RangePosition(1, 10, 1, 15), new RangePosition(5, 8, 5, 13) },
                            Content =
                            {
                                {
                                    "LocalDom",
                                    new DomainDef
                                    {
                                        Name = "LocalDom",
                                        NameLocations = { new RangePosition(3, 12, 3, 20) },
                                        TypeDef = new TextType { Length = 20, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }, SourceRange = new RangePosition(3, 23, 3, 30) }
                                    }
                                }
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Duplicate association name in topic",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS A =
                    END A;
                    CLASS B =
                    END B;
                    ASSOCIATION Assoc =
                        roleA -- A;
                        roleB -- B;
                    END Assoc;
                    ASSOCIATION Assoc =
                        roleC -- A;
                        roleD -- B;
                    END Assoc;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 2:10 An element with name Assoc already exists in the scope Topic."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(15, 4, 15, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "Topic",
                        new TopicDef
                        {
                            Name = "Topic",
                            NameLocations = { new RangePosition(1, 10, 1, 15), new RangePosition(14, 8, 14, 13) },
                            Content =
                            {
                                {
                                    "A",
                                    new ClassDef
                                    {
                                        Name = "A",
                                        NameLocations = { new RangePosition(2, 14, 2, 15), new RangePosition(3, 12, 3, 13) },
                                    }
                                },
                                {
                                    "B",
                                    new ClassDef
                                    {
                                        Name = "B",
                                        NameLocations = { new RangePosition(4, 14, 4, 15), new RangePosition(5, 12, 5, 13) },
                                    }
                                },
                                {
                                    "Assoc",
                                    new AssociationDef
                                    {
                                        Name = "Assoc",
                                        NameLocations = { new RangePosition(6, 20, 6, 25), new RangePosition(9, 12, 9, 17) },
                                        Content =
                                        {
                                            {
                                                "roleA",
                                                new AttributeDef
                                                {
                                                    Name = "roleA",
                                                    NameLocations = { new RangePosition(7, 12, 7, 17) },
                                                    TypeDef = new RoleType
                                                    {
                                                        Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "A" }, SourceRange = new RangePosition(7, 21, 7, 22) } } },
                                                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                                    },
                                                }
                                            },
                                            {
                                                "roleB",
                                                new AttributeDef
                                                {
                                                    Name = "roleB",
                                                    NameLocations = { new RangePosition(8, 12, 8, 17) },
                                                    TypeDef = new RoleType
                                                    {
                                                        Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "B" }, SourceRange = new RangePosition(8, 21, 8, 22) } } },
                                                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                                    },
                                                }
                                            },
                                        },
                                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    }
                                },
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Duplicate role name in association",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS A =
                    END A;
                    CLASS B =
                    END B;
                    ASSOCIATION Assoc =
                        role -- A;
                        role -- B;
                    END Assoc;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 7:8 An element with name role already exists in the scope Assoc."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(11, 4, 11, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "Topic",
                        new TopicDef
                        {
                            Name = "Topic",
                            NameLocations = { new RangePosition(1, 10, 1, 15), new RangePosition(10, 8, 10, 13) },
                            Content =
                            {
                                {
                                    "A",
                                    new ClassDef
                                    {
                                        Name = "A",
                                        NameLocations = { new RangePosition(2, 14, 2, 15), new RangePosition(3, 12, 3, 13) },
                                    }
                                },
                                {
                                    "B",
                                    new ClassDef
                                    {
                                        Name = "B",
                                        NameLocations = { new RangePosition(4, 14, 4, 15), new RangePosition(5, 12, 5, 13) },
                                    }
                                },
                                {
                                    "Assoc",
                                    new AssociationDef
                                    {
                                        Name = "Assoc",
                                        NameLocations = { new RangePosition(6, 20, 6, 25), new RangePosition(9, 12, 9, 17) },
                                        Content =
                                        {
                                            {
                                                "role",
                                                new AttributeDef
                                                {
                                                    Name = "role",
                                                    NameLocations = { new RangePosition(7, 12, 7, 16) },
                                                    TypeDef = new RoleType
                                                    {
                                                        Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "A" }, SourceRange = new RangePosition(7, 20, 7, 21) } } },
                                                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                                    },
                                                }
                                            },
                                        },
                                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    }
                                },
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Unit and domain with same name in model",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    m1 [m1];
                DOMAIN
                    m1 = TEXT*20;
            END Model.
            """,
            ExpectedLog: ["Compile error at line 1:6 An element with name m1 already exists in the scope Model."],
            RefHB: "3.5.4-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(5, 4, 5, 9) },
                URI = "http://example.com",
                Version = "1.0.0",
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
                Content =
                {
                    {
                        "m1",
                        new UnitDef
                        {
                            Term = "m1",
                            Name = "m1",
                            NameLocations = { new RangePosition(2, 12, 2, 14) },
                        }
                    }
                },
            }));

        yield return FullFile(new(
            "Local name shadows unqualified import",
            """
            INTERLIS 2.4;
            MODEL ModelA AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    MyDomain = TEXT*20;
            END ModelA.
            MODEL ModelB AT "http://example.com" VERSION "1.0.0" =
                IMPORTS UNQUALIFIED ModelA;
                DOMAIN
                    MyDomain = TEXT*10;
                TOPIC Topic =
                    CLASS ClassName =
                        Attr : MyDomain;
                    END ClassName;
                END Topic;
            END ModelB.
            """,
            ExpectedLog: ["Ambiguous 'reference 'MyDomain' from ModelB.Topic.ClassName' could be resolved to multiple targets: ModelB.MyDomain, ModelA.MyDomain"],
            RefHB: "3.5.4-11",
            AssertOutput: false,
            Ili2cDivergenceReason: "RefHB-silent (3.5.4 defines no local-vs-UNQUALIFIED-import precedence): we report it ambiguous, ili2c silently accepts (local wins)"));

        yield return FullFile(new(
            "Reference to model-level domain from within topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    ModelDomain = TEXT*20;
                TOPIC Topic =
                    CLASS ClassName =
                        Attr : ModelDomain;
                    END ClassName;
                END Topic;
            END Model.
            """,
            RefHB: "3.5.4-11",
            AssertOutput: false));

        yield return FullFile(new(
            "Topic-level name shadows model-level name",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    ModelDomain = TEXT*20;
                TOPIC Topic =
                    DOMAIN
                        ModelDomain = TEXT*10;
                    CLASS ClassName =
                        Attr : ModelDomain;
                    END ClassName;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Ambiguous 'reference 'ModelDomain' from Model.Topic.ClassName' could be resolved to multiple targets: Model.Topic.ModelDomain, Model.ModelDomain"],
            RefHB: "3.5.4-11",
            AssertOutput: false,
            Ili2cDivergenceReason: "RefHB 3.5.4-11 mandates this error (a local name must not collide with a name taken over from the superordinate element): we reject it, ili2c is lenient"));

        yield return FullFile(new(
            "Reference to model without IMPORTS",
            """
            INTERLIS 2.4;
            MODEL ModelA AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    MyDomain = TEXT*20;
            END ModelA.
            MODEL ModelB AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassName =
                        Attr : ModelA.MyDomain;
                    END ClassName;
                END Topic;
            END ModelB.
            """,
            ExpectedLog: ["Could not resolve 'reference 'ModelA.MyDomain' from ModelB.Topic.ClassName'"],
            RefHB: "3.5.4-12",
            AssertOutput: false));

        yield return FullFile(new(
            "Unqualified reference without UNQUALIFIED import",
            """
            INTERLIS 2.4;
            MODEL ModelA AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    MyDomain = TEXT*20;
            END ModelA.
            MODEL ModelB AT "http://example.com" VERSION "1.0.0" =
                IMPORTS ModelA;
                TOPIC Topic =
                    CLASS ClassName =
                        Attr : MyDomain;
                    END ClassName;
                END Topic;
            END ModelB.
            """,
            ExpectedLog: ["Could not resolve 'reference 'MyDomain' from ModelB.Topic.ClassName'"],
            RefHB: "3.5.4-12",
            AssertOutput: false));

        yield return FullFile(new(
            "Ambiguous unqualified reference from import",
            """
            INTERLIS 2.4;
            MODEL OtherName AT "foo:test" VERSION "123" =
                DOMAIN Name = TEXT;
            END OtherName.

            MODEL Name AT "foo:test" VERSION "123" =
                IMPORTS UNQUALIFIED OtherName;
                TOPIC Name =
                    CLASS Name =
                        Name : Name;
                    END Name;
                END Name;
            END Name.
            """,
            ExpectedLog: ["Ambiguous 'reference 'Name' from Name.Name.Name' could be resolved to multiple targets: Name.Name.Name, OtherName.Name"],
            RefHB: "3.5.4-12",
            AssertOutput: false));

        yield return FullFile(new(
            "Ambiguous unqualified reference from multiple imports",
            """
            INTERLIS 2.4;
            MODEL ModelA AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    SharedName = TEXT*20;
            END ModelA.
            MODEL ModelB AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    SharedName = TEXT*10;
            END ModelB.
            MODEL ModelC AT "http://example.com" VERSION "1.0.0" =
                IMPORTS UNQUALIFIED ModelA;
                IMPORTS UNQUALIFIED ModelB;
                TOPIC Topic =
                    CLASS ClassName =
                        Attr : SharedName;
                    END ClassName;
                END Topic;
            END ModelC.
            """,
            ExpectedLog: ["Ambiguous 'reference 'SharedName' from ModelC.Topic.ClassName' could be resolved to multiple targets: ModelA.SharedName, ModelB.SharedName"],
            RefHB: "3.5.4-12",
            AssertOutput: false,
            Ili2cDivergenceReason: "RefHB-silent (3.5.4 defines no resolution for the same unqualified name from several UNQUALIFIED imports): we report it ambiguous, ili2c silently accepts"));

        yield return FullFile(new(
            "Reference to element in different topic without qualification",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    DOMAIN
                        LocalDomain = TEXT*20;
                END TopicA;
                TOPIC TopicB =
                    CLASS ClassName =
                        Attr : LocalDomain;
                    END ClassName;
                END TopicB;
            END Model.
            """,
            ExpectedLog: ["Could not resolve 'reference 'LocalDomain' from Model.TopicB.ClassName'"],
            RefHB: "3.5.4-12",
            AssertOutput: false));

        yield return FullFile(new(
            "Reference resolution where Model, Topic and Class have same name",
            """
            INTERLIS 2.4;
            MODEL Name AT "foo:test" VERSION "123" =
                TOPIC Name =
                    CLASS Name =
                    END Name;

                    ASSOCIATION AssociationName =
                        Name -- Name;
                        Name2 -- Name;
                    END AssociationName;
                END Name;
            END Name.
            """,
            RefHB: "3.5.4-12",
            Expected: TestTools.Build(() =>
            {
                var nameClass = new ClassDef { Name = "Name" };
                var association = new AssociationDef
                {
                    Name = "AssociationName",
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    Content =
                    {
                        {
                            "Name",
                            new AttributeDef
                            {
                                Name = "Name",
                                TypeDef = new RoleType
                                {
                                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = nameClass, Path = { "Name" } } } },
                                }
                            }
                        },
                        {
                            "Name2",
                            new AttributeDef
                            {
                                Name = "Name2",
                                TypeDef = new RoleType
                                {
                                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = nameClass, Path = { "Name" } } } },
                                }
                            }
                        },
                    }
                };

                nameClass.AssociationAccess.Add("AssociationName", association);

                return new InterlisEnvironment
                {
                    Version = 2.4,
                    Content =
                    {
                        { InternalModel.Interlis.Name, InternalModel.Interlis },
                        {
                            "Name",
                            new ModelDef
                            {
                                Name = "Name",
                                URI = "foo:test",
                                Version = "123",
                                Content =
                                {
                                    {
                                        "Name",
                                        new TopicDef
                                        {
                                            Name = "Name",
                                            Content =
                                            {
                                                { "Name", nameClass },
                                                { "AssociationName", association },
                                            }
                                        }
                                    },
                                },
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                                },
                            }
                        },
                    },
                };
            })));

        yield return FullFile(new(
            "Projection view base name collides with attribute name",
            """
            INTERLIS 2.4;
            MODEL Model AT "foo:test" VERSION "123" =
                TOPIC Topic =
                    CLASS Class =
                        attr: TEXT;
                    END Class;

                    VIEW V PROJECTION OF base~Class; =
                    ATTRIBUTE
                        base := base->attr;
                    END V;
                END Topic;
            END Model.
            """,
            Description: """
                Base names ("Basissichten") are Bestandteilnamen — the same name category as attributes and roles — so a
                base name and an attribute of the same view share one namespace and must be unique. Here the projection
                base alias 'base' collides with the derived attribute 'base'. (Verified: ili2c rejects this as "can not
                declare several attributes bearing the same name".)
                """,
            // The base view 'base' is registered in the view's namespace and collides with the derived attribute
            // 'base', so it surfaces as the same duplicate-name diagnostic (reported at the view name) as any other
            // Bestandteilname clash.
            // Scope is "V" (not the full path) because the view is not yet attached to its topic while its own
            // members are being registered during construction.
            ExpectedLog: ["Compile error at line 8:13 An element with name base already exists in the scope V."],
            RefHB: "3.5.4",
            AssertOutput: false));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        {fragment}
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(NamespaceTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadModelDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitModelDef(p.modelDef()));
    }
}
