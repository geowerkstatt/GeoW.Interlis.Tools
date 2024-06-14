using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools;
using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools.CreateAST;

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
                                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.UNBOUND },
                                                Content =
                                                {
                                                    {
                                                        "roleA",
                                                        new AttributeDef
                                                        {
                                                            Name = "roleA",
                                                            TypeDef = new RoleType
                                                            {
                                                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.UNBOUND },
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
                                                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.UNBOUND },
                                                                Targets = { new RestrictedRef { Target = classB } },
                                                            }
                                                        }
                                                    },
                                                    {
                                                        "attr",
                                                        new AttributeDef
                                                        {
                                                            Name = "attr",
                                                            TypeDef = new TypeDef
                                                            {
                                                                Name = "",
                                                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                                                Definition = "TEXT*12",
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
