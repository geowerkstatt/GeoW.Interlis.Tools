using Geowerkstatt.Interlis.Tools.AST;
using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools.CreateAST;
using Geowerkstatt.Interlis.Tools.AST.Types;

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
                        new ModelDef { Name = "ModelName", URI = "foo.test", Version = "123" }
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
            Extends = classA,
        };

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
                        Content =
                        {
                            { "BaseTopic", baseTopic },
                            {
                                "OtherTopic",
                                new TopicDef
                                {
                                    Name = "OtherTopic",
                                    Extends = baseTopic,
                                }
                            },
                            {
                                "Topic",
                                new TopicDef
                                {
                                    Name = "Topic",
                                    Extends = baseTopic,
                                    Content =
                                    {
                                        { "A", classA },
                                        { "B", classB },
                                        {
                                            "C",
                                            new AssociationDef
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
                                                                Targets = { new RestrictedRef { Target = classA } },
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
                                                                Targets = { new RestrictedRef { Target = classB } },
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
                                            }
                                        },
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
    public void ReadFileWithDomains()
    {
        // Text domain
        var textDomain = new DomainDef { Name = "text", TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } } };
        var text2Domain = new DomainDef { Name = "text2", TypeDef = new TypeRef { Extends = textDomain.TypeDef, Cardinality = new Cardinality { Min = 1, Max = Cardinality.Unbound } } };

        // Oid domain
        var yoloOidDomain = new DomainDef { Name = "yoloOid", TypeDef = new OidType { TypeDef = new OidAnyType(), Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } } };
        var itemIdDomain = new DomainDef { Name = "item_id", TypeDef = new OidType { TypeDef = new NumericType { Min = 100000, Max = 999999, Precision = 0 }, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } } };
        var basketIdDomain = new DomainDef { Name = "basket_id", TypeDef = new OidType { Extends = yoloOidDomain.TypeDef, TypeDef = new TextType { Length = 6 }, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } } };

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
                Extends = colorDomain.TypeDef,
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
                Extends = enhancedColorDomain.TypeDef,
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
                TargetEnumeration = (EnumerationType)superEnhancedColorDomain.TypeDef,
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
                VertexType = point3dDomain.TypeDef,
                OverlapTolerance = 0.0,
                LineForm = { "STRAIGHTS" },
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }
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
                            {
                                "TopicName",
                                new TopicDef
                                {
                                    Name = "TopicName",
                                    BasketOidType = basketIdDomain.TypeDef,
                                    OidType = itemIdDomain.TypeDef,
                                    Content =
                                    {
                                        { "point3d", point3dDomain },
                                        { "surface", surfaceDomain },
                                        {
                                            "ClassName",
                                            new ClassDef
                                            {
                                                Name = "ClassName",
                                                OidType = itemIdDomain.TypeDef,
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
                                                                Target = new RestrictedRef { Target = text2Domain },
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
                                                                Target = new RestrictedRef { Target = point3dDomain },
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
                                                                VertexType = point3dDomain.TypeDef,
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
                                                                Target = new RestrictedRef { Target = surfaceDomain },
                                                            }
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

                TOPIC TopicName =
                    BASKET OID AS basket_id;
                    OID AS item_id;

                    DOMAIN
                        point3d = MANDATORY COORD 0 .. 99, 100 .. 199, 200 .. 299;
                        surface = SURFACE WITH (STRAIGHTS) VERTEX point3d;

                    CLASS ClassName =
                        OID AS item_id;

                        TextAttr : text2;
                        Points : point3d;
                        Lines : DIRECTED MULTIPOLYLINE WITH (STRAIGHTS, ARCS) VERTEX point3d WITHOUT OVERLAPS >0.01;
                        Surface : surface;
                    END ClassName;
                END TopicName;
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
        var actual = new InterlisReader().ReadRule(new StringReader(input), parseRule);
        Assert.IsInstanceOfType(actual, expected?.GetType());
        AssertDeepEqual(expected, actual);
    }

    private static void AssertDeepEqual(object expected, object actual)
    {
        expected.WithDeepEqual(actual)
            .IgnoreProperty<IInterlisDefinition>(d => d.Parent) // Ignore parent property to break circular references
            .IgnoreProperty<IInterlisDefinition>(d => d.FullyQualifiedName) // Ignore calculated property
            .Assert();
    }
}
