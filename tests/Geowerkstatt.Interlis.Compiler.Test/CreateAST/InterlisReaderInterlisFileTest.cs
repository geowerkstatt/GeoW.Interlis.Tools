using Geowerkstatt.Interlis.Tools.AST;
using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools.CreateAST;
using Geowerkstatt.Interlis.Tools.AST.Types;
using Microsoft.Extensions.Logging;
using Compiler.Test.CreateAST;

namespace Geowerkstatt.Interlis.Tools;

[TestClass]
public class InterlisReaderInterlisFileTest
{
    [TestMethod]
    public void ReadFileNoModels()
    {
        AssertReadFile("INTERLIS 2.4;", new InterlisFile());
    }

    [TestMethod]
    public void ReadFileWithModel()
    {
        AssertReadFile("""
            INTERLIS 2.4;
            MODEL ModelName AT "foo.test" VERSION "123" =
            END ModelName.
            """,
            new InterlisFile
            {
                Content =
                {
                    {
                        "ModelName",
                        new ModelDef { Name = "ModelName", URI = "foo.test", Version = "123", Imports = { { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } } }
                    },
                }
            });
    }

    [TestMethod]
    public void ReadFileWithModelWithDocComment()
    {
        AssertReadFile("""
            INTERLIS 2.4;
            /** I am a doc comment */
            MODEL ModelName AT "foo.test" VERSION "123" =
            END ModelName.
            """,
            new InterlisFile
            {
                Content =
                {
                    {
                        "ModelName",
                        new ModelDef
                        {
                            Name = "ModelName",
                            URI = "foo.test",
                            Version = "123",
                            Imports = { { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } },
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
            new InterlisFile
            {
                Content =
                {
                    {
                        "ModelName",
                        new ModelDef
                        {
                            Name = "ModelName",
                            URI = "foo.test",
                            Version = "123",
                            Imports = { { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } },
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

        var expected = new InterlisFile
        {
            Content =
            {
                {
                    "Model",
                    new ModelDef
                    {
                        Name = "Model",
                        URI = "foo.test",
                        Version = "123",
                        Imports = { { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } },
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
                Extends = new Reference<TypeDef> { Target = textDomain.TypeDef, Path = { "text" } },
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
                Extends = new Reference<TypeDef> { Target = yoloOidDomain.TypeDef, Path = { "yoloOid" } },
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
                Extends = new Reference<TypeDef> { Target = colorDomain.TypeDef, Path = { "color" } },
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
                Extends = new Reference<TypeDef> { Target = enhancedColorDomain.TypeDef, Path = { "enhancedColor" } },
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
                TargetEnumeration = new Reference<EnumerationType> { Target = (EnumerationType)superEnhancedColorDomain.TypeDef, Path = { "superEnhancedColor" } },
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
                VertexType = new Reference<TypeDef> { Target = point3dDomain.TypeDef, Path = { "point3d" } },
                OverlapTolerance = 0.0,
                LineForm = { "STRAIGHTS" },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
            }
        };

        // Class / Structure reference targets
        var structType = new ClassDef { Name = "structType", IsStructure = true };
        var person = new ClassDef { Name = "Person" };

        var expected = new InterlisFile
        {
            Content =
            {
                {
                    "ModelName",
                    new ModelDef
                    {
                        Name = "ModelName",
                        URI = "foo:test",
                        Version = "123",
                        Imports = { { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } },
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
                                    BasketOidType = new Reference<TypeDef> { Target = basketIdDomain.TypeDef, Path = { "basket_id" } },
                                    OidType = new Reference<TypeDef> { Target = itemIdDomain.TypeDef, Path = { "item_id" } },
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
                                                OidType = new Reference<TypeDef> { Target = itemIdDomain.TypeDef, Path = { "item_id" } },
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
                                                                VertexType = new Reference<TypeDef> { Target = point3dDomain.TypeDef, Path = { "point3d" } },
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
                                                                Extends = new Reference<TypeDef> { Target = ((DomainDef)Interlis24AstReferenceResolverVisitor.InternalInterlisModel.Content["HALIGNMENT"]).TypeDef, Path = { "INTERLIS", "HALIGNMENT" } },
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
                                                                Extends = new Reference<TypeDef> { Target = ((DomainDef)Interlis24AstReferenceResolverVisitor.InternalInterlisModel.Content["VALIGNMENT"]).TypeDef, Path = { "INTERLIS", "VALIGNMENT" } },
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
        var lengthUnit = new UnitDef { Name = "Length", Term = "Length" };
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

        var expected = new InterlisFile
        {
            Content =
            {
                {
                    "ModelName",
                    new ModelDef
                    {
                        Name = "ModelName",
                        URI = "foo:test",
                        Version = "123",
                        Imports = { { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } },
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
            Imports = { { "INTERLIS", (true, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } },
            Content = { }
        };

        var modelB = new ModelDef
        {
            Name = "Model_B",
            URI = "foo:test",
            Version = "123",
            Imports = { { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } },
            Content = { }
        };

        var expected = new InterlisFile
        {
            Content =
            {
                { "Model_A", modelA },
                { "Model_B", modelB },
                {
                    "Model_C",
                    new ModelDef
                    {
                        Name = "Model_C",
                        URI = "foo:test",
                        Version = "123",
                        Imports = {
                            { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) },
                            { "Model_A", (false, modelA) },
                            { "Model_B", (true, modelB) },
                            { "Unknown_Model", (false, null) },
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

        var formattedType = new FormattedType
        {
            Min = "[000]",
            Max = "[090]",
            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            BasedOn = new Reference<ClassDef> { Target = structure, Path = { "Struct" } },
        };

        var formattedType2 = new FormattedType
        {
            Min = "[012]",
            Max = "[034]",
            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
            FormatBaseType = new Reference<FormattedType> { Target = formattedType, Path = { "Format" } },
        };

        var expected = new InterlisFile
        {
            Content =
            {
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
                            {
                                "Format",
                                new DomainDef { Name = "Format", TypeDef = formattedType }
                            },
                            {
                                "Format2",
                                new DomainDef { Name = "Format2", TypeDef = formattedType2 }
                            },
                        },
                        Imports = { { "INTERLIS", (false, Interlis24AstReferenceResolverVisitor.InternalInterlisModel) } },
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

    private static void AssertReadFile(string input, InterlisFile expected)
    {
        var actual = new InterlisReader().ReadFile(new StringReader(input));
        AssertDeepEqual(expected, actual);
    }

    internal static void AssertReadRule<TResult>(string input, object? expected, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule)
    {
        var (actual, _) = new InterlisReader().ReadRule(new StringReader(input), parseRule);
        Assert.IsInstanceOfType(actual, expected?.GetType());
        AssertDeepEqual(expected, actual);
    }

    internal static List<string> GetLogMessages<TResult>(string input, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule)
    {
        var logProvider = new TestLoggerProvider();
        var loggerFactory = LoggerFactory.Create(b => b.AddProvider(logProvider));

        try
        {
            var (actual, _) = new InterlisReader(loggerFactory).ReadRule(new StringReader(input), parseRule);
        }
        catch (Exception)
        {
        }

        return logProvider.GetMessages();
    }

    private static void AssertDeepEqual(object expected, object actual)
    {
        expected.WithDeepEqual(actual)
            .IgnoreProperty<IInterlisDefinition>(d => d.Parent) // Ignore parent property to break circular references
            .IgnoreProperty(p => p.DeclaringType.IsGenericType
                    && typeof(Reference<object>).GetGenericTypeDefinition() == p.DeclaringType.GetGenericTypeDefinition()
                    && (nameof(Reference<object>.Source).Equals(p.Name) // Ignore reference source to break circular references
                        || nameof(Reference<object>.MapTarget).Equals(p.Name))) // Ignore Func property
            .IgnoreProperty<IInterlisDefinition>(d => d.FullyQualifiedName) // Ignore calculated property
            .IgnoreCircularReferences()
            .Assert();
    }
}
