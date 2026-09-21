using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class InterlisFileTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return FullFile(new(
            "No models",
            "INTERLIS 2.4;",
            RefHB: "3.3",
            Expected: new InterlisEnvironment { Version = 2.4, Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } } }));

        yield return FullFile(new(
            "Unsupported version is rejected",
            "INTERLIS 2.3;",
            ExpectedLog: ["Unsupported INTERLIS version 2.3 at 1:9-1:12. Only version 2.4 is supported."],
            RefHB: "3.3-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Missing INTERLIS header is rejected",
            """
            MODEL ModelName AT "foo:test" VERSION "1" =
            END ModelName.
            """,
            ExpectedLog:
            [
                "Compile error at 1:0-1:5 mismatched input 'MODEL' expecting 'INTERLIS'.",
                "Unsupported INTERLIS version (null) at 1:0-1:5. Only version 2.4 is supported.",
            ],
            RefHB: "3.3-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Missing semicolon after version is rejected",
            "INTERLIS 2.4",
            ExpectedLog: ["Compile error at 1:12-1:13 missing ';' at '<EOF>'."],
            RefHB: "3.3-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Multiple models",
            """
            INTERLIS 2.4;
            MODEL ModelA AT "foo:test" VERSION "1" =
            END ModelA.

            MODEL ModelB AT "foo:test" VERSION "1" =
            END ModelB.
            """,
            RefHB: "3.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Model",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "http://foo.test" VERSION "123" =
            END ModelName.
            """,
            Expected: new InterlisEnvironment
            {
                Version = 2.4,
                Content =
                {
                    { InternalModel.Interlis.Name, InternalModel.Interlis },
                    {
                        "ModelName",
                        new ModelDef
                        {
                            Name = "ModelName",
                            URI = "http://foo.test",
                            Version = "123",
                            Imports =
                            {
                                { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                            }
                        }
                    },
                }
            }));

        yield return FullFile(new(
            "Model with documentation comment",
            """
            /** Ignored doc comment because "interlis" rule does not read doc comments */
            INTERLIS 2.4;
            /** I am a doc comment */
            MODEL ModelName AT "http://foo.test" VERSION "123" =
            END ModelName.
            """,
            Expected: new InterlisEnvironment
            {
                Version = 2.4,
                Content =
                {
                    { InternalModel.Interlis.Name, InternalModel.Interlis },
                    {
                        "ModelName",
                        new ModelDef
                        {
                            Name = "ModelName",
                            URI = "http://foo.test",
                            Version = "123",
                            Imports =
                            {
                                { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                            },
                            DocComments = { "/** I am a doc comment */" }
                        }
                    }
                }
            }));

        yield return FullFile(new(
            "Model with topic and class",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "http://foo.test" VERSION "123" =
                CLASS ClassName =
                END ClassName;
                TOPIC TopicName =
                    CLASS TopicClassName =
                    END TopicClassName;
                END TopicName;
            END ModelName.
            """,
            ExpectedLog: ["Type check error in 'ModelName.ClassName' at 3:4-4:18: must be declared ABSTRACT because it is not part of a topic."],
            Expected: new InterlisEnvironment
            {
                Version = 2.4,
                Content =
                {
                    { InternalModel.Interlis.Name, InternalModel.Interlis },
                    {
                        "ModelName",
                        new ModelDef
                        {
                            Name = "ModelName",
                            URI = "http://foo.test",
                            Version = "123",
                            Imports =
                            {
                                { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                            },
                            Content =
                            {
                                { "ClassName", new ClassDef { Name = "ClassName" } },
                                {
                                    "TopicName",
                                    new TopicDef
                                    {
                                        Name = "TopicName",
                                        Content =
                                        {
                                            { "TopicClassName", new ClassDef { Name = "TopicClassName" } }
                                        },
                                    }
                                },
                            }
                        }
                    }
                }
            }));

        yield return FullFile(new(
            "Associations",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://foo.test" VERSION "123" =
                TOPIC Topic EXTENDS BaseTopic =
                    CLASS A =
                    END A;

                    CLASS B EXTENDS A =
                    END B;

                    ASSOCIATION C =
                        roleA -- A;
                        roleB -- B;
                    ATTRIBUTE
                        attr : TEXT*12;
                    END C;
                END Topic;

                TOPIC BaseTopic = 
                END BaseTopic;

                TOPIC OtherTopic EXTENDS Model.BaseTopic =
                END OtherTopic;
            END Model.
            """,
            Expected: TestTools.Build(() =>
            {
                var classA = new ClassDef
                {
                    Name = "A",
                };

                var classB = new ClassDef
                {
                    Name = "B",
                    Extends = new Reference<ClassDef> { Target = classA, Path = { "A" } },
                };

                var associationC = new AssociationDef
                {
                    Name = "C",
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    Content =
                    {
                        {
                            "roleA",
                            new AttributeDef
                            {
                                Name = "roleA",
                                TypeDef = new RoleType
                                {
                                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = classA, Path = { "A" } } } },
                                }
                            }
                        },
                        {
                            "roleB",
                            new AttributeDef
                            {
                                Name = "roleB",
                                TypeDef = new RoleType
                                {
                                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    Targets = { new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = classB, Path = { "B" } } } },
                                }
                            }
                        },
                        {
                            "attr",
                            new AttributeDef
                            {
                                Name = "attr",
                                TypeDef = new TextType
                                {
                                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                                    Length = 12,
                                }
                            }
                        }
                    }
                };

                classA.AssociationAccess.Add("C", associationC);
                classB.AssociationAccess.Add("C", associationC);

                var baseTopic = new TopicDef
                {
                    Name = "BaseTopic",
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
                                URI = "http://foo.test",
                                Version = "123",
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                                },
                                Content =
                                {
                                    { "BaseTopic", baseTopic },
                                    {
                                        "OtherTopic",
                                        new TopicDef
                                        {
                                            Name = "OtherTopic",
                                            Extends = new Reference<TopicDef> { Target = baseTopic, Path = { "Model", "BaseTopic" } },
                                        }
                                    },
                                    {
                                        "Topic",
                                        new TopicDef
                                        {
                                            Name = "Topic",
                                            Extends = new Reference <TopicDef> { Target = baseTopic, Path = { "BaseTopic" } },
                                            Content =
                                            {
                                                { "A", classA },
                                                { "B", classB },
                                                { "C", associationC },
                                            },
                                        }
                                    },
                                }
                            }
                        }
                    }
                };
            }),
            Ili2cDivergenceReason: "we accept this model (associations with a forward-referenced base topic), ili2c rejects it"));

        yield return FullFile(new(
            "Attribute type references",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                DOMAIN
                    text = TEXT*12;
                    text2 EXTENDS text = MANDATORY;

                    color = (red, green, blue);
                    enhancedColor EXTENDS color = (red (yellow, orange), green(lightGreen, darkGreen : FINAL));
                    superEnhancedColor EXTENDS enhancedColor =
                        (
                            /** wild */ red.yellow (lightYellow, darkYellow),
                            /** is that even a color? */ transparent, green (FINAL)
                        : FINAL);
                    allColor = ALL OF superEnhancedColor;

                    yoloOid = OID ANY;
                    item_id = OID 100000 .. 999999;
                    basket_id EXTENDS yoloOid = OID TEXT*6;

                STRUCTURE structType =
                END structType;

                TOPIC TopicName =
                    BASKET OID AS basket_id;
                    OID AS item_id;

                    DOMAIN
                        point3d = MANDATORY COORD 0 .. 99, 100 .. 199, 200 .. 299;
                        surface = SURFACE WITH (STRAIGHTS) VERTEX point3d;

                    CLASS Person =
                    END Person;

                    CLASS ClassName =
                        OID AS item_id;

                        TextAttr : text2;
                        Points : point3d;
                        Lines : DIRECTED MULTIPOLYLINE WITH (STRAIGHTS, ARCS) VERTEX point3d WITHOUT OVERLAPS >0.01;
                        Surface : surface;
                        struct : structType;
                        restrictedStruct : structType RESTRICTION ( ModelName.structType );
                        externalReference : REFERENCE TO (EXTERNAL) ExternalClassName;
                        reference : REFERENCE TO Person;
                        horizontalAlignment : HALIGNMENT;
                        verticalAlignment : VALIGNMENT;
                    PARAMETER
                        paramDomain : text2;
                        paramStruct : structType;
                    END ClassName;
                END TopicName;
            END ModelName.
            """,
            ExpectedLog: ["Could not resolve 'reference 'ExternalClassName' from ModelName.TopicName.ClassName' at 43:56-43:73"],
            Expected: TestTools.Build(() =>
            {
                // Text domain
                var textDomain = new DomainDef
                {
                    Name = "text",
                    TypeDef = new TextType
                    {
                        Length = 12,
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
                    }
                };
                var text2Domain = new DomainDef
                {
                    Name = "text2",
                    TypeDef = new TypeRef
                    {
                        Extends = new Reference<DomainDef> { Target = textDomain, Path = { "text" } },
                        Cardinality = new Cardinality { Min = 1, Max = Cardinality.Unbound },
                    },
                };

                // Oid domain
                var yoloOidDomain = new DomainDef
                {
                    Name = "yoloOid",
                    TypeDef = new OidType
                    {
                        Value = new OidType.AnyOid(),
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
                    }
                };
                var itemIdDomain = new DomainDef
                {
                    Name = "item_id",
                    TypeDef = new OidType
                    {
                        Value = new OidType.ValueRange { Type = new DecimalType { Min = 100000, Max = 999999, Precision = 0 } },
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
                    }
                };
                var basketIdDomain = new DomainDef
                {
                    Name = "basket_id",
                    TypeDef = new OidType
                    {
                        Extends = new Reference<DomainDef> { Target = yoloOidDomain, Path = { "yoloOid" } },
                        Value = new OidType.ValueRange { Type = new TextType { Length = 6 } },
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
                    }
                };

                // Enumeration domain
                var colorDomain = new DomainDef
                {
                    Name = "color",
                    TypeDef = new EnumerationType
                    {
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                        Values =
                        {
                            new EnumerationTreeNode { Name = "red" },
                            new EnumerationTreeNode { Name = "green" },
                            new EnumerationTreeNode { Name = "blue" },
                        }
                    }
                };
                var enhancedColorDomain = new DomainDef
                {
                    Name = "enhancedColor",
                    TypeDef = new EnumerationType
                    {
                        Extends = new Reference<DomainDef> { Target = colorDomain, Path = { "color" } },
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                        Values =
                        {
                            new EnumerationTreeNode
                            {
                                Name = "red",
                                SubValues =
                                {
                                    new EnumerationTreeNode { Name = "yellow" },
                                    new EnumerationTreeNode { Name = "orange" },
                                }
                            },
                            new EnumerationTreeNode
                            {
                                Name = "green",
                                SubValues =
                                {
                                    new EnumerationValuesList(isFinal: true)
                                    {
                                        new EnumerationTreeNode { Name = "lightGreen" },
                                        new EnumerationTreeNode { Name = "darkGreen" },
                                    }
                                }
                            },
                        }
                    }
                };
                var superEnhancedColorDomain = new DomainDef
                {
                    Name = "superEnhancedColor",
                    TypeDef = new EnumerationType
                    {
                        Extends = new Reference<DomainDef> { Target = enhancedColorDomain, Path = { "enhancedColor" } },
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                        Values =
                        {
                            new EnumerationValuesList(isFinal: true)
                            {
                                new EnumerationTreeNode
                                {
                                    Name = "red",
                                    SubValues =
                                    {
                                        new EnumerationTreeNode
                                        {
                                            Name = "yellow",
                                            FromDottedName = true,
                                            DocComments = { "/** wild */" },
                                            SubValues =
                                            {
                                                new EnumerationTreeNode { Name = "lightYellow" },
                                                new EnumerationTreeNode { Name = "darkYellow" },
                                            }
                                        },
                                    }
                                },
                                new EnumerationTreeNode
                                {
                                    Name = "transparent",
                                    DocComments = { "/** is that even a color? */" },
                                },
                                new EnumerationTreeNode
                                {
                                    Name = "green",
                                    SubValues = { new EnumerationValuesList(isFinal: true) }
                                },
                            }
                        }
                    }
                };
                var allColorDomain = new DomainDef
                {
                    Name = "allColor",
                    TypeDef = new EnumerationValuesType
                    {
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                        TargetEnumeration = new Reference<DomainDef> { Target = superEnhancedColorDomain, Path = { "superEnhancedColor" } },
                        LeafsOnly = false,
                    }
                };

                // Geometry domain
                var point3dDomain = new DomainDef
                {
                    Name = "point3d",
                    TypeDef = new CoordType
                    {
                        Axis = { new DecimalType { Min = 0, Max = 99, Precision = 0 }, new DecimalType { Min = 100, Max = 199, Precision = 0 }, new DecimalType { Min = 200, Max = 299, Precision = 0 } },
                        Cardinality = new Cardinality { Min = 1, Max = Cardinality.Unbound }
                    },
                };
                var surfaceDomain = new DomainDef
                {
                    Name = "surface",
                    TypeDef = new SurfaceType
                    {
                        VertexType = new Reference<DomainDef> { Target = point3dDomain, Path = { "point3d" } },
                        LineForms = { new Reference<LineFormTypeDef> { Target = (LineFormTypeDef)InternalModel.Interlis.Content["STRAIGHTS"], Path = { "INTERLIS", "STRAIGHTS" } } },
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
                    }
                };

                // Class / Structure reference targets
                var structType = new ClassDef { Name = "structType", IsStructure = true };
                var person = new ClassDef { Name = "Person" };

                return new InterlisEnvironment
                {
                    Version = 2.4,
                    Content =
                    {
                        { InternalModel.Interlis.Name, InternalModel.Interlis },
                        {
                            "ModelName",
                            new ModelDef
                            {
                                Name = "ModelName",
                                URI = "foo:test",
                                Version = "123",
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                                },
                                Content =
                                {
                                    { "text", textDomain },
                                    { "text2", text2Domain },

                                    { "color", colorDomain },
                                    { "enhancedColor", enhancedColorDomain },
                                    { "superEnhancedColor", superEnhancedColorDomain },
                                    { "allColor", allColorDomain },

                                    { "yoloOid", yoloOidDomain },
                                    { "item_id", itemIdDomain },
                                    { "basket_id", basketIdDomain },

                                    { "structType", structType },
                                    {
                                        "TopicName",
                                        new TopicDef
                                        {
                                            Name = "TopicName",
                                            BasketOidType = new Reference<DomainDef> { Target = basketIdDomain, Path = { "basket_id" } },
                                            OidType = new Reference<DomainDef> { Target = itemIdDomain, Path = { "item_id" } },
                                            Content =
                                            {
                                                { "point3d", point3dDomain },
                                                { "surface", surfaceDomain },
                                                { "Person", person },
                                                {
                                                    "ClassName",
                                                    new ClassDef
                                                    {
                                                        Name = "ClassName",
                                                        OidType = new Reference<DomainDef> { Target = itemIdDomain, Path = { "item_id" } },
                                                        Content =
                                                        {
                                                            {
                                                                "TextAttr",
                                                                new AttributeDef
                                                                {
                                                                    Name = "TextAttr",
                                                                    TypeDef = new TypeRef
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Extends = new Reference<DomainDef> { Target = text2Domain, Path = { "text2" } },
                                                                    }
                                                                }
                                                            },
                                                            {
                                                                "Points",
                                                                new AttributeDef
                                                                {
                                                                    Name = "Points",
                                                                    TypeDef = new TypeRef
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Extends = new Reference<DomainDef> { Target = point3dDomain, Path = { "point3d" } },
                                                                    }
                                                                }
                                                            },
                                                            {
                                                                "Lines",
                                                                new AttributeDef
                                                                {
                                                                    Name = "Lines",
                                                                    TypeDef = new PolyLineType
                                                                    {
                                                                        IsMultiGeometry = true,
                                                                        IsDirected = true,
                                                                        VertexType = new Reference<DomainDef> { Target = point3dDomain, Path = { "point3d" } },
                                                                        WithoutOverlaps = new WithoutOverlapsDef.Explicit { Tolerance = 0.01 },
                                                                        LineForms = { new Reference<LineFormTypeDef> { Target = (LineFormTypeDef)InternalModel.Interlis.Content["STRAIGHTS"], Path = { "INTERLIS", "STRAIGHTS" } }, new Reference<LineFormTypeDef> { Target = (LineFormTypeDef)InternalModel.Interlis.Content["ARCS"], Path = { "INTERLIS", "ARCS" } } },
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                    }
                                                                }
                                                            },
                                                            {
                                                                "Surface",
                                                                new AttributeDef
                                                                {
                                                                    Name = "Surface",
                                                                    TypeDef = new TypeRef
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Extends = new Reference<DomainDef> { Target = surfaceDomain, Path = { "surface" } },
                                                                    }
                                                                }
                                                            },
                                                            {
                                                                "struct",
                                                                new AttributeDef
                                                                {
                                                                    Name = "struct",
                                                                    TypeDef = new ObjectType
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Targets = [new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = structType, Path = { "structType" } } }],
                                                                    }
                                                                }
                                                            },
                                                            {
                                                                "restrictedStruct",
                                                                new AttributeDef
                                                                {
                                                                    Name = "restrictedStruct",
                                                                    TypeDef = new ObjectType
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Targets = [new RestrictedRef
                                                                        {
                                                                            Value = new Reference<IInterlisDefinition> { Target = structType, Path = { "structType" } },
                                                                            Restrictions = { new Reference<IInterlisDefinition> { Target = structType, Path = { "ModelName", "structType" } } }
                                                                        }],
                                                                    },
                                                                }
                                                            },
                                                            {
                                                                "externalReference",
                                                                new AttributeDef
                                                                {
                                                                    Name = "externalReference",
                                                                    TypeDef = new ReferenceType
                                                                    {
                                                                        Properties = { Property.External },                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "ExternalClassName" } } },
                                                                    },
                                                                }
                                                            },
                                                            {
                                                                "reference",
                                                                new AttributeDef
                                                                {
                                                                    Name = "reference",
                                                                    TypeDef = new ReferenceType
                                                                    {                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = person, Path = { "Person" } } },
                                                                    },
                                                                }
                                                            },
                                                            {
                                                                "horizontalAlignment",
                                                                new AttributeDef
                                                                {
                                                                    Name = "horizontalAlignment",
                                                                    TypeDef = new TypeRef
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Extends = new Reference<DomainDef> { Target = (DomainDef)InternalModel.Interlis.Content["HALIGNMENT"], Path = { "INTERLIS", "HALIGNMENT" } },
                                                                    },
                                                                }
                                                            },
                                                            {
                                                                "verticalAlignment",
                                                                new AttributeDef
                                                                {
                                                                    Name = "verticalAlignment",
                                                                    TypeDef = new TypeRef
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Extends = new Reference<DomainDef> { Target = (DomainDef)InternalModel.Interlis.Content["VALIGNMENT"], Path = { "INTERLIS", "VALIGNMENT" } },
                                                                    },
                                                                }
                                                            },
                                                            // A parameter is written with the same attrTypeDef rule as an attribute (RefHB 3.10.2),
                                                            // so its named types are classified the same way: a domain becomes a TypeRef alias and
                                                            // a structure becomes a contained-substructure ObjectType.
                                                            {
                                                                "paramDomain",
                                                                new ParameterDef
                                                                {
                                                                    Name = "paramDomain",
                                                                    TypeDef = new TypeRef
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Extends = new Reference<DomainDef> { Target = text2Domain, Path = { "text2" } },
                                                                    },
                                                                }
                                                            },
                                                            {
                                                                "paramStruct",
                                                                new ParameterDef
                                                                {
                                                                    Name = "paramStruct",
                                                                    TypeDef = new ObjectType
                                                                    {
                                                                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                        Targets = [new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = structType, Path = { "structType" } } }],
                                                                    },
                                                                }
                                                            },
                                                        },
                                                    }
                                                }
                                            },
                                        }
                                    },
                                }
                            }
                        }
                    }
                };
            })));

        yield return FullFile(new(
            "Imports",
            """
            INTERLIS 2.4;
            MODEL Model_A AT "foo:test" VERSION "123" =
                IMPORTS UNQUALIFIED INTERLIS;
            END Model_A.

            MODEL Model_B AT "foo:test" VERSION "123" =
            END Model_B.

            MODEL Model_C AT "foo:test" VERSION "123" =
                IMPORTS INTERLIS, Model_A;
                IMPORTS UNQUALIFIED Model_B;
                IMPORTS Unknown_Model;
            END Model_C.
            """,
            ExpectedLog: ["Could not resolve 'reference 'Unknown_Model' from Model_C' at 12:12-12:25"],
            Expected: TestTools.Build(() =>
            {
                var modelA = new ModelDef
                {
                    Name = "Model_A",
                    URI = "foo:test",
                    Version = "123",
                    Imports =
                    {
                        { InternalModel.Interlis.Name, (true, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                    },
                    Content = { }
                };

                var modelB = new ModelDef
                {
                    Name = "Model_B",
                    URI = "foo:test",
                    Version = "123",
                    Imports =
                    {
                        { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                    },
                    Content = { }
                };

                return new InterlisEnvironment
                {
                    Version = 2.4,
                    Content =
                    {
                        { InternalModel.Interlis.Name, InternalModel.Interlis },
                        { "Model_A", modelA },
                        { "Model_B", modelB },
                        {
                            "Model_C",
                            new ModelDef
                            {
                                Name = "Model_C",
                                URI = "foo:test",
                                Version = "123",
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) },
                                    { "Model_A", (false, new Reference<ModelDef> { Target = modelA, Path = { "Model_A" } }) },
                                    { "Model_B", (true, new Reference<ModelDef> { Target = modelB, Path = { "Model_B" } }) },
                                    { "Unknown_Model", (false, new Reference<ModelDef> { Target = null, Path = { "Unknown_Model" } }) }
                                },
                                Content = {}
                            }
                        }
                    }
                };
            })));

        yield return FullFile(new(
            "Function call",
            """
            INTERLIS 2.4;

            TYPE MODEL Text_V2 (en) AT "http://www.interlis.ch/models" VERSION "2023-05-25" =
                FUNCTION endsWith(val: TEXT; suffix: TEXT): BOOLEAN;
            END Text_V2.

            MODEL ModelName AT "foo:test" VERSION "123" =
                IMPORTS UNQUALIFIED Text_V2;
                DOMAIN specialText = TEXT*12 CONSTRAINTS EndsWithPoint : endsWith(THIS, ".");
            END ModelName.
            """,
            Expected: TestTools.Build(() =>
            {
                var functionDef = new FunctionDef
                {
                    Name = "endsWith",
                    ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                    Arguments =
                    {
                        new FunctionArgument { Name = "val", Type = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                        new FunctionArgument { Name = "suffix", Type = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                    },
                };

                var functionModel = new ModelDef
                {
                    Name = "Text_V2",
                    Type = ModelDef.ModelType.Type,
                    URI = "http://www.interlis.ch/models",
                    Version = "2023-05-25",
                    Language = "en",
                    Imports =
                    {
                        { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                    },
                    Content = { { "endsWith", functionDef } }
                };

                return new InterlisEnvironment
                {
                    Version = 2.4,
                    Content =
                    {
                        { InternalModel.Interlis.Name, InternalModel.Interlis },
                        { "Text_V2", functionModel },
                        {
                            "ModelName",
                            new ModelDef
                            {
                                Name = "ModelName",
                                URI = "foo:test",
                                Version = "123",
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) },
                                    { "Text_V2", (true, new Reference<ModelDef> { Target = functionModel, Path = { "Text_V2" } }) }
                                },
                                Content =
                                {
                                    {
                                        "specialText",
                                        new DomainDef
                                        {
                                            Name = "specialText",
                                            TypeDef = new TextType
                                            {
                                                Length = 12,
                                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                                Constraints =
                                                {
                                                    {
                                                        "EndsWithPoint",
                                                        new DomainConstraint
                                                        {
                                                            Name = "EndsWithPoint",
                                                            SourceRange = new RangePosition(8, 45, 8, 80),
                                                            Condition = new FunctionCall
                                                            {
                                                                FunctionDef = new Reference<FunctionDef> { Target = functionDef, Path = { "endsWith" } },
                                                                Arguments =
                                                                {
                                                                    new PathExpression
                                                                    {
                                                                        Path = { new KeyWordPathElement { Value = KeyWordPathElement.KeyWord.This } },
                                                                    },
                                                                    new TextConstant
                                                                    {
                                                                        Value = ".",
                                                                    }
                                                                },
                                                            }
                                                        }
                                                    },
                                                }
                                            }
                                        }
                                    },
                                },
                            }
                        }
                    },
                };
            })));
    }

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(InterlisFileTest), GetCases());

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }
}
