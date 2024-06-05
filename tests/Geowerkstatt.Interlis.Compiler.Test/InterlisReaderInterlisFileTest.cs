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
    public void ReadFileWithModeWithDocComment()
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
    public void ReadFileWithModeTopicAndClass()
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
                            URI = "foo.test", Version = "123",
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
                            {
                                "Topic",
                                new TopicDef
                                {
                                    Name = "Topic",
                                    Content =
                                    {
                                        { "A", classA },
                                        { "B", classB },
                                        {
                                            "C",
                                            new AssociationDef
                                            {
                                                Name = "C",
                                                RoleDefs =
                                                {
                                                    new AttributeDef
                                                    {
                                                        Name = "roleA",
                                                        TypeDef = new RoleType
                                                        {
                                                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.UNBOUND },
                                                            Targets = { new RestrictedRef { Target = classA } },
                                                        }
                                                    },
                                                    new AttributeDef
                                                    {
                                                        Name = "roleB",
                                                        TypeDef = new RoleType
                                                        {
                                                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.UNBOUND },
                                                            Targets = { new RestrictedRef { Target = classB } },
                                                        }
                                                    },
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
                TOPIC Topic =
                    CLASS A =
                        Attr : TEXT*12;
                    END A;
                    CLASS B =
                    END B;
                    ASSOCIATION C =
                        roleA -- A;
                        roleB -- B;
                        !!ATTRIBUTE
                        !!Attr_Association : TEXT*13
                    END C;
                END Topic;
            END Model.
            """, expected);


    }

    private static void AssertReadFile(string input, InterlisFile expected)
    {
        var actual = new InterlisReader().ReadFile(new StringReader(input));

        expected.WithDeepEqual(actual)
            .IgnoreProperty<IInterlisDefinition>(d => d.Parent) // Ignore parent property to break circular references
            .Assert();
    }

    internal static void AssertReadRule<TResult>(string input, object? expected, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule)
    {
        var actual = new InterlisReader().ReadRule(new StringReader(input), parseRule);
        Assert.IsInstanceOfType(actual, expected?.GetType());
        expected.ShouldDeepEqual(actual);
    }
}
