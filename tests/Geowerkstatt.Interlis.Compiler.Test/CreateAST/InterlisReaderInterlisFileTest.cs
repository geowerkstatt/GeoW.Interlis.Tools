using Geowerkstatt.Interlis.Compiler.AST;
using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Compiler.CreateAST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using Microsoft.Extensions.Logging;
using Compiler.Test.CreateAST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderInterlisFileTest
{
    [TestMethod]
    public void ReadFileNoModels()
    {
        AssertReadFile("INTERLIS 2.4;", new InterlisEnvironment { Version = 2.4, Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } } });
    }

    [TestMethod]
    public void ReadFileWithModel()
    {
        AssertReadFile("""
            INTERLIS 2.4;
            MODEL ModelName AT "foo.test" VERSION "123" =
            END ModelName.
            """,
            new InterlisEnvironment
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
                            URI = "foo.test",
                            Version = "123",
                            Imports =
                            {
                                { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                            }
                        }
                    },
                }
            });
    }

    [TestMethod]
    public void ReadFileWithModelWithDocComment()
    {
        AssertReadFile("""
            /** Ignored doc comment because "interlis" rule does not read doc comments */
            INTERLIS 2.4;
            /** I am a doc comment */
            MODEL ModelName AT "foo.test" VERSION "123" =
            END ModelName.
            """,
            new InterlisEnvironment
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
                            URI = "foo.test",
                            Version = "123",
                            Imports =
                            {
                                { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                            },
                            DocComments = { "/** I am a doc comment */" }
                        }
                    }
                }
            });
    }

    [TestMethod]
    public void ReadFileWithModelTopicAndClass()
    {
        AssertReadFile("""
            INTERLIS 2.4;
            MODEL ModelName AT "foo.test" VERSION "123" =
                CLASS ClassName =
                END ClassName;
                TOPIC TopicName =
                    CLASS TopicClassName =
                    END TopicClassName;
                END TopicName;
            END ModelName.
            """,
            new InterlisEnvironment
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
                            URI = "foo.test",
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
            });
    }

    [TestMethod]
    public void ReadFileWithAssociation()
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

        var expected = new InterlisEnvironment
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
                        URI = "foo.test",
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

        AssertReadFile("""
            INTERLIS 2.4;
            MODEL Model AT "foo.test" VERSION "123" =
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
            """, expected);
    }

    [TestMethod]
    public void ReadFileAttributeTypeReferences()
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
                TypeDef = new OidAnyType(),
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
            }
        };
        var itemIdDomain = new DomainDef
        {
            Name = "item_id",
            TypeDef = new OidType
            {
                TypeDef = new NumericType { Min = 100000, Max = 999999, Precision = 0 },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
            }
        };
        var basketIdDomain = new DomainDef
        {
            Name = "basket_id",
            TypeDef = new OidType
            {
                Extends = new Reference<DomainDef> { Target = yoloOidDomain, Path = { "yoloOid" } },
                TypeDef = new TextType { Length = 6 },
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
                            Name = "blue",
                            SubValues = { new EnumerationValuesList(isFinal: true) }
                        },
                    }
                }
            }
        };
        var allColorDomain = new DomainDef
        {
            Name = "allColor",
            TypeDef = new EnumerationAllOfType
            {
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                TargetEnumeration = new Reference<DomainDef> { Target = superEnhancedColorDomain, Path = { "superEnhancedColor" } },
            }
        };

        // Geometry domain
        var point3dDomain = new DomainDef
        {
            Name = "point3d",
            TypeDef = new CoordType
            {
                Axis = { new NumericType { Min = 0, Max = 99, Precision = 0 }, new NumericType { Min = 100, Max = 199, Precision = 0 }, new NumericType { Min = 200, Max = 299, Precision = 0 } },
                Cardinality = new Cardinality { Min = 1, Max = Cardinality.Unbound }
            },
        };
        var surfaceDomain = new DomainDef
        {
            Name = "surface",
            TypeDef = new SurfaceType
            {
                VertexType = new Reference<DomainDef> { Target = point3dDomain, Path = { "point3d" } },
                OverlapTolerance = 0.0,
                LineForm = { "STRAIGHTS" },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
            }
        };

        // Class / Structure reference targets
        var structType = new ClassDef { Name = "structType", IsStructure = true };
        var person = new ClassDef { Name = "Person" };

        var expected = new InterlisEnvironment
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
                                                            TypeDef = new ReferenceType
                                                            {
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = text2Domain, Path = { "text2" } } },
                                                            }
                                                        }
                                                    },
                                                    {
                                                        "Points",
                                                        new AttributeDef
                                                        {
                                                            Name = "Points",
                                                            TypeDef = new ReferenceType
                                                            {
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = point3dDomain, Path = { "point3d" } } },
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
                                                                OverlapTolerance = 0.01,
                                                                LineForm = { "STRAIGHTS", "ARCS" },
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                            }
                                                        }
                                                    },
                                                    {
                                                        "Surface",
                                                        new AttributeDef
                                                        {
                                                            Name = "Surface",
                                                            TypeDef = new ReferenceType
                                                            {
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = surfaceDomain, Path = { "surface" } } },
                                                            }
                                                        }
                                                    },
                                                    {
                                                        "struct",
                                                        new AttributeDef
                                                        {
                                                            Name = "struct",
                                                            TypeDef = new ReferenceType
                                                            {
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = structType, Path = { "structType" } } },
                                                            }
                                                        }
                                                    },
                                                    {
                                                        "restrictedStruct",
                                                        new AttributeDef
                                                        {
                                                            Name = "restrictedStruct",
                                                            TypeDef = new ReferenceType
                                                            {
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                Target = new RestrictedRef
                                                                {
                                                                    Value = new Reference<IInterlisDefinition> { Target = structType, Path = { "structType" } },
                                                                    Restrictions = { new Reference<IInterlisDefinition> { Target = structType, Path = { "ModelName", "structType" } } }
                                                                },
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
                                                                Properties = { Property.External },
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
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
                                                            {
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
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

        AssertReadFile("""
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
                            /** is that even a color? */ transparent, blue (FINAL)
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
                    END ClassName;
                END TopicName;
            END ModelName.
            """, expected);
    }

    [TestMethod]
    public void ReadFileWithUnits()
    {
        var lengthUnit = new UnitDef { Name = "Length", Term = "Length", Properties = { Property.Abstract } };
        var meterUnit = new UnitDef { Name = "m", Term = "Meter", Extends = new Reference<UnitDef> { Target = lengthUnit, Path = { "Length" } } };

        var heightDomain = new DomainDef
        {
            Name = "Height",
            TypeDef = new NumericType
            {
                Min = 0,
                Max = 10,
                Precision = -2,
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                Unit = new Reference<UnitDef> { Target = meterUnit, Path = { "m" } },
            }
        };

        var expected = new InterlisEnvironment
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
                            { "Length", lengthUnit },
                            { "m", meterUnit },
                            { "Height", heightDomain },
                        }
                    }
                }
            }
        };

        AssertReadFile("""
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                UNIT
                    Length (ABSTRACT);
                    Meter [m] EXTENDS Length;

                DOMAIN
                    Height = 0.00 .. 10.00 [m];
            END ModelName.
            """, expected);
    }

    [TestMethod]
    public void ReadFileWithImports()
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

        var expected = new InterlisEnvironment
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

        AssertReadFile("""
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
            """, expected);
    }

    [TestMethod]
    public void ReadFileWithFormattedType()
    {
        var structure = new ClassDef
        {
            Name = "Struct",
            IsStructure = true,
            Content =
            {
                {
                    "Value",
                    new AttributeDef
                    {
                        Name = "Value",
                        TypeDef = new NumericType { Min = 0, Max = 90, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 } }
                    }
                }
            }
        };

        var formattedDomain = new DomainDef
        {
            Name = "Format",
            TypeDef = new FormattedType
            {
                Min = "[000]",
                Max = "[090]",
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                BasedOn = new Reference<ClassDef> { Target = structure, Path = { "Struct" } },
            }
        };

        var formattedDomain2 = new DomainDef
        {
            Name = "Format2",
            TypeDef = new FormattedType
            {
                Min = "[012]",
                Max = "[034]",
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                FormatBaseType = new Reference<DomainDef> { Target = formattedDomain, Path = { "Format" } },
            }
        };

        var expected = new InterlisEnvironment
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
                        Content =
                        {
                            { "Struct", structure },
                            { "Format", formattedDomain },
                            { "Format2", formattedDomain2 },
                        },
                        Imports =
                        {
                            { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                        },
                    }
                }
            }
        };

        AssertReadFile("""
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                STRUCTURE Struct =
                    Value : 0 .. 90;
                END Struct;

                DOMAIN
                    Format = FORMAT BASED ON Struct ( "[" Value / 3 "]" ) "[000]" .. "[090]";
                    Format2 = FORMAT Format "[012]" .. "[034]";
            END ModelName.
            """, expected);
    }

    [TestMethod]
    public void ReadFileWithDateTime()
    {
        var interlis = InternalModel.Interlis;

        var expected = new InterlisEnvironment
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
                        Content =
                        {
                            {
                                "Struct",
                                new ClassDef
                                {
                                    Name = "Struct",
                                    IsStructure = true,
                                    Content =
                                    {
                                        {
                                            "Date",
                                            new AttributeDef
                                            {
                                                Name = "Date",
                                                TypeDef = new TypeRef
                                                {
                                                    Extends = new Reference<DomainDef> { Target = (DomainDef)interlis.Content["XMLDate"], Path = { "INTERLIS", "XMLDate" } },
                                                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                },
                                            }
                                        },
                                        {
                                            "Time",
                                            new AttributeDef
                                            {
                                                Name = "Time",
                                                TypeDef = new TypeRef
                                                {
                                                    Extends = new Reference<DomainDef> { Target = (DomainDef)interlis.Content["XMLTime"], Path = { "INTERLIS", "XMLTime" } },
                                                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                },
                                            }
                                        },
                                        {
                                            "DateTime",
                                            new AttributeDef
                                            {
                                                Name = "DateTime",
                                                TypeDef = new TypeRef
                                                {
                                                    Extends = new Reference<DomainDef> { Target = (DomainDef)interlis.Content["XMLDateTime"], Path = { "INTERLIS", "XMLDateTime" } },
                                                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                },
                                            }
                                        },
                                    },
                                }
                            },
                        },
                        Imports =
                        {
                            { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                        },
                    }
                }
            },
        };

        AssertReadFile("""
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                STRUCTURE Struct =
                    Date : DATE;
                    Time : TIMEOFDAY;
                    DateTime : DATETIME;
                END Struct;
            END ModelName.
            """, expected);
    }

    [TestMethod]
    public void ReadFileWithViews()
    {
        var interlis = InternalModel.Interlis;

        var expected = new InterlisEnvironment
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
                        Content =
                        {
                            {
                                "Topic",
                                new TopicDef
                                {
                                    Name = "Topic",
                                    Content =
                                    {
                                        {
                                            "Class",
                                            new ClassDef
                                            {
                                                Name = "Class",
                                                Content =
                                                {
                                                    {
                                                        "Attr",
                                                        new AttributeDef
                                                        {
                                                            Name = "Attr",
                                                            TypeDef = new TextType
                                                            {
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                            },
                                                        }
                                                    },
                                                },
                                            }
                                        },
                                    }
                                }
                            },
                        },
                        Imports =
                        {
                            { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                        },
                    }
                }
            },
        };

        AssertReadFile("""
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                TOPIC Topic =
                    CLASS Class =
                        Attr : TEXT;
                    END Class;

                    !! Empty view with ATTRIBUTE keyword
                    VIEW EmptyView
                      PROJECTION OF base~Class;
                      =
                      ATTRIBUTE
                    END EmptyView;
                END Topic;
            END ModelName.
            """, expected);
    }

    [TestMethod]
    public void ReferenceResolutionModelTopiClassSameName()
    {
        var interlis = InternalModel.Interlis;
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

        var expected = new InterlisEnvironment
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

        AssertReadFile("""
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
            """, expected);
    }

    [TestMethod]
    public void ReferenceResolutionConflictWithUnqualifiedImport()
    {
        var logProvider = new TestLoggerProvider();
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().AddProvider(logProvider));
        var reader = new InterlisReader(loggerFactory);
        reader.ReadFile(new StringReader("""
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
            """));

        Assert.AreEqual("Ambiguous 'reference 'Name' from Name.Name' could be resolved to multiple targets: Name.Name.Name, OtherName.Name", logProvider.GetMessages().FirstOrDefault());
    }

    [TestMethod]
    public void ReadFileWithFunctionCall()
    {
        var functionDef = new FunctionDef
        {
            Name = "endsWith",
            ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
        };

        var functionModel = new ModelDef
        {
            Name = "Text_V2",
            URI = "http://www.interlis.ch/models",
            Version = "2023-05-25",
            Language = "en",
            Imports =
            {
                { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
            },
            Content = { { "endsWith", functionDef } }
        };

        var expected = new InterlisEnvironment
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
                                            new DomainConstraint
                                            {
                                                Name = "EndsWithPoint",
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
                                                            ReturnType = new TextType(),
                                                        }
                                                    },
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

        AssertReadFile("""
            INTERLIS 2.4;

            TYPE MODEL Text_V2 (en) AT "http://www.interlis.ch/models" VERSION "2023-05-25" =
                FUNCTION endsWith(val: TEXT; suffix: TEXT): BOOLEAN;
            END Text_V2.

            MODEL ModelName AT "foo:test" VERSION "123" =
                IMPORTS UNQUALIFIED Text_V2;
                DOMAIN specialText = TEXT*12 CONSTRAINTS EndsWithPoint : endsWith(THIS, ".");
            END ModelName.
            """, expected);
    }

    internal static void AssertReadFile(string input, InterlisEnvironment expected)
    {
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var actual = new InterlisReader(loggerFactory).ReadFile(new StringReader(input));
        AssertDeepEqual(expected, actual, deepEqual => deepEqual
                .IgnoreProperty<IInterlisDefinition>(p => p.NameLocations) // Ignore NameLocations because it adds too much clutter in tests for whole interlis files
                .IgnoreProperty<ISourceRange>(p => p.SourceRange)
                .IgnoreProperty(p => p.DeclaringType.IsGenericType
                    && typeof(Reference<IInterlisDefinition>).GetGenericTypeDefinition() == p.DeclaringType.GetGenericTypeDefinition()
                    && nameof(Reference<IInterlisDefinition>.ReferenceLocation).Equals(p.Name))
                );
    }

    internal static void AssertReadRule<TResult>(string input, object? expected, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule)
    {
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var actual = new InterlisReader(loggerFactory).ReadRule(new StringReader(input), parseRule);
        Assert.IsInstanceOfType(actual, expected?.GetType());
        AssertDeepEqual(expected, actual);
    }

    internal static List<string> GetLogMessages<TResult>(string input, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule)
    {
        var logProvider = new TestLoggerProvider();
        var loggerFactory = LoggerFactory.Create(b => b.AddProvider(logProvider));

        try
        {
            var actual = new InterlisReader(loggerFactory).ReadRule(new StringReader(input), parseRule);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Exception thrown during parsing: {ex}");
        }

        return logProvider.GetMessages();
    }

    private static void AssertDeepEqual(object expected, object actual, Func<CompareSyntax<object, object>, CompareSyntax<object, object>>? configureDeepEqual = null)
    {
        var deepEqualAssert = expected.WithDeepEqual(actual)
            .IgnoreProperty<IInterlisDefinition>(d => d.Parent) // Ignore parent property to break circular references
            .IgnoreProperty(p => p.DeclaringType.IsGenericType
                    && typeof(Reference<IInterlisDefinition>).GetGenericTypeDefinition() == p.DeclaringType.GetGenericTypeDefinition()
                    && (nameof(Reference<IInterlisDefinition>.Source).Equals(p.Name) // Ignore reference source to break circular references
                        || nameof(Reference<IInterlisDefinition>.MapTarget).Equals(p.Name) // Ignore Func property
                        || nameof(Reference<IInterlisDefinition>.OnResolved).Equals(p.Name))) // Ignore Callback property
            .IgnoreProperty<IInterlisDefinition>(d => d.FullyQualifiedName) // Ignore calculated property
            .IgnoreProperty<IInterlisDefinitionContainer>(d => d.ContainerReferences) // Easy access collection for references
            .IgnoreCircularReferences();

        if (configureDeepEqual != null)
        {
            deepEqualAssert = configureDeepEqual(deepEqualAssert);
        }

        deepEqualAssert.Assert();
    }
}
