using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class FormattedTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Formatted range",
            "\"00\"..\"99\"",
            Description: """
                The bare Min..Max form (RefHB 3.8.6-3, third alternative) carries no format of its own; standalone there
                is no format to interpret the bounds by, so the wrapped comparison agrees with ili2c on the rejection.
                The legal use — restricting an inherited formatted domain — is pinned by the full-file case 'Bare
                formatted range restricting an inherited format is accepted'.
                """,
            RefHB: "3.8.6-1",
            Expected: new FormattedType
            {
                Min = "00",
                Max = "99",
                SourceRange = new RangePosition(0, 0, 0, 10),
            }));

        yield return FullFile(new(
            "Bare formatted range restricting an inherited format is accepted",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                STRUCTURE Struct =
                    Value : 0 .. 90;
                END Struct;

                DOMAIN
                    Base = FORMAT BASED ON Struct ( Value / 2 ) "00" .. "90";
                    Narrow EXTENDS Base = "10" .. "80";
            END ModelName.
            """,
            RefHB: "3.8.6-8",
            AssertOutput: false));

        yield return FullFile(new(
            "Bare formatted range without any format is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassName =
                        attr : "00".."99";
                    END ClassName;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.ClassName -> attr' at 5:12-5:30: a formatted range without a format definition must extend a formatted domain."],
            RefHB: "3.8.6-3",
            AssertOutput: false));

        yield return Rule(new(
            "Formatted based on structure",
            "FORMAT BASED ON GregorianDate ( Year/4 \"-\" Month/2 \"-\" Day/2 )",
            RefHB: "3.8.6-3",
            Expected: new FormattedType
            {
                BasedOn = new Reference<ClassDef> { Path = { new("GregorianDate") } },
                Format = new FormatDef
                {
                    Components =
                    {
                        new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Year") } }, Position = 4 },
                        new FormatSeparator { Value = "-" },
                        new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Month") } }, Position = 2 },
                        new FormatSeparator { Value = "-" },
                        new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Day") } }, Position = 2 },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 62),
            }));

        yield return Rule(new(
            "Formatted based on structure with value range",
            "FORMAT BASED ON Struct ( Year/4 \"-\" Month/2 ) \"0000-00\" .. \"9999-12\"",
            RefHB: "3.8.6-3",
            Expected: new FormattedType
            {
                BasedOn = new Reference<ClassDef> { Path = { new("Struct") } },
                Min = "0000-00",
                Max = "9999-12",
                Format = new FormatDef
                {
                    Components =
                    {
                        new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Year") } }, Position = 4 },
                        new FormatSeparator { Value = "-" },
                        new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Month") } }, Position = 2 },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 68),
            }));

        yield return Rule(new(
            "Formatted range of domain",
            "FORMAT INTERLIS.XMLDateTime \"2000-01-01T00:00:00.000\" .. \"2000-12-31T23:59:59.999\"",
            RefHB: "3.8.6-3",
            Expected: new FormattedType
            {
                Min = "2000-01-01T00:00:00.000",
                Max = "2000-12-31T23:59:59.999",
                FormatBaseType = new Reference<DomainDef> { Path = { new("INTERLIS"), new("XMLDateTime") } },
                SourceRange = new RangePosition(0, 0, 0, 82),
            }));

        yield return Rule(new(
            "Formatted format definition with inheritance",
            "FORMAT BASED ON Struct ( INHERITANCE \"[\" Value / 3 \"]\" )",
            RefHB: "3.8.6-4",
            Expected: new FormattedType
            {
                BasedOn = new Reference<ClassDef> { Path = { new("Struct") } },
                Format = new FormatDef
                {
                    Inheritance = true,
                    Components =
                    {
                        new FormatSeparator { Value = "[" },
                        new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Value") } }, Position = 3 },
                        new FormatSeparator { Value = "]" },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 56),
            }));

        yield return Rule(new(
            "Formatted numeric attribute reference without position",
            "FORMAT BASED ON Struct ( Year )",
            RefHB: "3.8.6-5",
            Expected: new FormattedType
            {
                BasedOn = new Reference<ClassDef> { Path = { new("Struct") } },
                Format = new FormatDef
                {
                    Components =
                    {
                        new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Year") } } },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 31),
            }));

        yield return Rule(new(
            "Formatted structure attribute reference via formatted domain",
            "FORMAT BASED ON Struct ( SubAttr / SomeDomain )",
            RefHB: "3.8.6-5",
            Expected: new FormattedType
            {
                BasedOn = new Reference<ClassDef> { Path = { new("Struct") } },
                Format = new FormatDef
                {
                    Components =
                    {
                        new FormatBaseAttribute
                        {
                            Attribute = new Reference<AttributeDef> { Path = { new("SubAttr") } },
                            FormattedDomain = new Reference<DomainDef> { Path = { new("SomeDomain") } },
                        },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 47),
            }));

        yield return FullFile(new(
            "Formatted based on custom structure",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                STRUCTURE Struct =
                    Value : 0 .. 90;
                END Struct;

                DOMAIN
                    Format = FORMAT BASED ON Struct ( "[" Value / 3 "]" ) "[000]" .. "[090]";
                    Format2 = FORMAT Format "[012]" .. "[034]";
            END ModelName.
            """,
            Expected: TestTools.Build(() =>
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
                                TypeDef = new DecimalType { Min = 0, Max = 90, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 } }
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
                        BasedOn = new Reference<ClassDef> { Target = structure, Path = { new("Struct") } },
                        Format = new FormatDef
                        {
                            Components =
                            {
                                new FormatSeparator { Value = "[" },
                                new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Target = (AttributeDef)structure.Content["Value"], Path = { new("Value") } }, Position = 3 },
                                new FormatSeparator { Value = "]" },
                            },
                        },
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
                        FormatBaseType = new Reference<DomainDef> { Target = formattedDomain, Path = { new("Format") } },
                    }
                };

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
                                Content =
                                {
                                    { "Struct", structure },
                                    { "Format", formattedDomain },
                                    { "Format2", formattedDomain2 },
                                },
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { new(InternalModel.Interlis.Name) } }) }
                                },
                            }
                        }
                    }
                };
            }),
            Ili2cDivergenceReason: "ili2c's format parser mishandles a leading separator before the first attribute reference and rejects correctly formatted bounds (verified: the same format without the leading separator is accepted); RefHB 3.8.6-4 explicitly allows a leading NonNum-String, so we accept."));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                CLASS ClassName =
                    attr : {fragment};
                END ClassName;
            END Topic;
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(FormattedTypeTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadFormattedType(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitFormattedType(p.formattedType()));
    }
}
