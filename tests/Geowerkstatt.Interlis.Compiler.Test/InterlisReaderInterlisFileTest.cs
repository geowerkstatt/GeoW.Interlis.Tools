using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools;
using DeepEqual.Syntax;

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
                Children =
                {
                    new ModelDef
                    {
                        FullyQualifiedName = new Identifier { Model = "ModelName" },
                        URI = "foo.test",
                        Version = "123"
                    }
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
                Children =
                {
                    new ModelDef
                    {
                        FullyQualifiedName = new Identifier { Model = "ModelName" },
                        URI = "foo.test",
                        Version = "123",
                        DocComments = { "/** I am a doc comment */" }
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
                Children =
                {
                    new ModelDef
                    {
                        FullyQualifiedName = new Identifier { Model = "ModelName" },
                        URI = "foo.test", Version = "123",
                        Children =
                        {
                            new ClassDef
                            {
                                FullyQualifiedName = new Identifier
                                {
                                    Model = "ModelName",
                                    Class = "ClassName",
                                }
                            },
                            new TopicDef
                            {
                                FullyQualifiedName = new Identifier
                                {
                                    Model = "ModelName",
                                    Topic = "TopicName",
                                },
                                Children =
                                {
                                    new ClassDef
                                    {
                                        FullyQualifiedName = new Identifier
                                        {
                                            Model = "ModelName",
                                            Topic = "TopicName",
                                            Class = "TopicClassName",
                                        }
                                    }
                                },
                            },
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
            FullyQualifiedName = new Identifier { Model = "Model", Topic = "Topic", Class = "A" }
        };

        var classB = new ClassDef
        {
            FullyQualifiedName = new Identifier { Model = "Model", Topic = "Topic", Class = "B" }
        };

        var expected = new InterlisFile
        {
            Children =
            {
                new ModelDef
                {
                    FullyQualifiedName = new Identifier { Model = "Model" },
                    URI = "foo.test", Version = "123",
                    Children =
                    {
                        new TopicDef
                        {
                            FullyQualifiedName = new Identifier { Model = "Model", Topic = "Topic" },
                            Children =
                            {
                                classA,
                                classB,
                                new AssociationDef
                                {
                                    FullyQualifiedName = new Identifier { Model = "Model", Topic = "Topic", Class = "C" },
                                    RoleDefs =
                                    {
                                        new AttributeDef
                                        {
                                            FullyQualifiedName = new Identifier { Model = "Model", Topic = "Topic", Class = "C", LeafElementName = "roleA" },
                                            TypeDef = new ReferenceType
                                            {
                                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.UNBOUND },
                                                Target = classA,
                                            }
                                        },
                                        new AttributeDef
                                        {
                                            FullyQualifiedName = new Identifier { Model = "Model", Topic = "Topic", Class = "C", LeafElementName = "roleB" },
                                            TypeDef = new ReferenceType
                                            {
                                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.UNBOUND },
                                                Target = classB,
                                            }
                                        },
                                    }
                                },
                            },
                        },
                    }
                }
            }
        };

        AssertReadFile("""
            INTERLIS 2.4;
            MODEL Model AT "foo.test" VERSION "123" =
                TOPIC Topic =
                    CLASS A =
                    END A;
                    CLASS B =
                    END B;
                    ASSOCIATION C =
                        roleA -- A;
                        roleB -- B;
                    END C;
                END Topic;
            END Model.
            """, expected);


    }

    private void AssertReadFile(string input, InterlisFile expected)
    {
        var actual = new InterlisReader().ReadFile(new StringReader(input));
        expected.ShouldDeepEqual(actual);
    }
}
