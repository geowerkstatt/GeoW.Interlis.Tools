using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class DateTimeTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Date attribute",
            "Attr : DATE;",
            RefHB: "3.8.7-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Extends = new Reference<DomainDef> { Path = { "INTERLIS", "XMLDate" } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                },
            }));

        yield return Rule(new(
            "Time attribute",
            "Attr : TIMEOFDAY;",
            RefHB: "3.8.7-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Extends = new Reference<DomainDef> { Path = { "INTERLIS", "XMLTime" } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                },
            }));

        yield return Rule(new(
            "Date time attribute",
            "Attr : DATETIME;",
            RefHB: "3.8.7-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Extends = new Reference<DomainDef> { Path = { "INTERLIS", "XMLDateTime" } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                },
            }));

        yield return Rule(new(
            "Datetime format range attribute",
            "Start : FORMAT INTERLIS.XMLDateTime \"2000-01-01T00:00:00.000\" .. \"2005-12-31T23:59:59.999\";",
            RefHB: "3.8.7-20",
            Expected: new AttributeDef
            {
                Name = "Start",
                NameLocations = { new RangePosition(0, 0, 0, 5) },
                TypeDef = new FormattedType
                {
                    FormatBaseType = new Reference<DomainDef> { Path = { "INTERLIS", "XMLDateTime" }, SourceRange = new RangePosition(0, 15, 0, 35) },
                    Min = "2000-01-01T00:00:00.000",
                    Max = "2005-12-31T23:59:59.999",
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 8, 0, 90),
                },
            }));

        yield return FullFile(new(
            "Datetime format range worked example (Projekt)",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                TOPIC Topic =
                    CLASS Projekt =
                        Start: FORMAT INTERLIS.XMLDateTime "2000-01-01T00:00:00.000" .. "2005-12-31T23:59:59.999";
                        Ende: FORMAT INTERLIS.XMLDateTime "2002-01-01T00:00:00.000" .. "2007-12-31T23:59:59.999";
                    END Projekt;
                END Topic;
            END ModelName.
            """,
            RefHB: "3.8.7-20",
            AssertOutput: false));

        yield return FullFile(new(
            "Read file with Datetime",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                STRUCTURE Struct =
                    Date : DATE;
                    Time : TIMEOFDAY;
                    DateTime : DATETIME;
                END Struct;
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
                                                        Extends = new Reference<DomainDef> { Target = (DomainDef)InternalModel.Interlis.Content["XMLDate"], Path = { "INTERLIS", "XMLDate" } },
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
                                                        Extends = new Reference<DomainDef> { Target = (DomainDef)InternalModel.Interlis.Content["XMLTime"], Path = { "INTERLIS", "XMLTime" } },
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
                                                        Extends = new Reference<DomainDef> { Target = (DomainDef)InternalModel.Interlis.Content["XMLDateTime"], Path = { "INTERLIS", "XMLDateTime" } },
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
            }));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                CLASS ClassName =
                    {fragment}
                END ClassName;
            END Topic;
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(DateTimeTypeTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadAttributeDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitAttributeDef(p.attributeDef()));
    }
}
