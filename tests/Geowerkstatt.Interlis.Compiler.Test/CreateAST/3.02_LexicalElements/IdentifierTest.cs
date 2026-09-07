using Geowerkstatt.Interlis.Compiler.AST;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class IdentifierTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Invalid identifier",
            """
            MODEL snake-case AT "http://example.com" VERSION "1.0.0" =
            END snake-case.
            """,
            ExpectedLog: ["Compile error at line 1:11 mismatched input '-' expecting {'(', 'AT', 'NOINCREMENTALTRANSFER'}."],
            RefHB: "3.2.2-1",
            Expected: new ModelDef
            {
                Name = "snake",
                NameLocations = { new RangePosition(0, 6, 0, 11) },
                Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) } },
            }));

        yield return Rule(new(
            "Reserved identifier",
            """
            MODEL ARCS AT "http://example.com" VERSION "1.0.0" =
            END ARCS.
            """,
            ExpectedLog: ["Compile error at line 1:6 mismatched input 'ARCS' expecting IDENTIFIER."],
            RefHB: "3.2.2-1",
            Expected: null));

        yield return Rule(new(
            "Identifier reserved because of compatibility with older INTERLIS versions",
            """
            MODEL TABLE AT "http://example.com" VERSION "1.0.0" =
            END TABLE.
            """,
            ExpectedLog: ["Compile error at line 1:6 mismatched input 'TABLE' expecting IDENTIFIER."],
            RefHB: "3.2.2-1",
            Expected: null,
            Ili2cDivergenceReason: "we reserve TABLE for compatibility with older INTERLIS and reject it as a name, ili2c accepts it"));

        yield return Rule(new(
            "Identifier must not start with a digit",
            """
            MODEL 2Model AT "http://example.com" VERSION "1.0.0" =
            END 2Model.
            """,
            ExpectedLog: [
                "Compile error at line 1:6 extraneous input '2' expecting IDENTIFIER.",
                "Compile error at line 2:4 extraneous input '2' expecting IDENTIFIER.",
            ],
            RefHB: "3.2.2-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 7, 0, 12), new RangePosition(1, 5, 1, 10) },
                Imports = { { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) } },
                URI = "http://example.com",
                Version = "1.0.0",
            }));

        yield return Rule(new(
            "Identifier must not start with an underscore",
            """
            MODEL _Model AT "http://example.com" VERSION "1.0.0" =
            END _Model.
            """,
            ExpectedLog: [
                "Compile error at line 1:6 extraneous input '_' expecting IDENTIFIER.",
                "Compile error at line 2:4 extraneous input '_' expecting IDENTIFIER.",
            ],
            RefHB: "3.2.2-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 7, 0, 12), new RangePosition(1, 5, 1, 10) },
                Imports = { { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) } },
                URI = "http://example.com",
                Version = "1.0.0",
            }));

        yield return Rule(new(
            "Identifier rejecting a dot as a special character",
            """
            MODEL my.model AT "http://example.com" VERSION "1.0.0" =
            END my.model.
            """,
            ExpectedLog: ["Compile error at line 1:8 mismatched input '.' expecting {'(', 'AT', 'NOINCREMENTALTRANSFER'}."],
            RefHB: "3.2.2-1",
            Expected: new ModelDef
            {
                Name = "my",
                NameLocations = { new RangePosition(0, 6, 0, 8) },
                Imports = { { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) } },
            }));

        yield return Rule(new(
            "Identifiers are case sensitive",
            """
            MODEL Foo AT "http://example.com" VERSION "1.0.0" =
            END foo.
            """,
            ExpectedLog: ["Compile error at line 2:4 Start name 'Foo' and end name 'foo' do not match."],
            RefHB: "3.2.2-1",
            Expected: new ModelDef
            {
                Name = "Foo",
                NameLocations = { new RangePosition(0, 6, 0, 9), new RangePosition(1, 4, 1, 7) },
                Imports = { { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) } },
                URI = "http://example.com",
                Version = "1.0.0",
            }));

        yield return Rule(new(
            "Valid identifier with letters, digits and underscores",
            """
            MODEL MyModel_2 AT "http://example.com" VERSION "1.0.0" =
            END MyModel_2.
            """,
            ExpectedLog: null,
            RefHB: "3.2.2-3",
            Expected: new ModelDef
            {
                Name = "MyModel_2",
                NameLocations = { new RangePosition(0, 6, 0, 15), new RangePosition(1, 4, 1, 13) },
                Imports = { { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) } },
                URI = "http://example.com",
                Version = "1.0.0",
            }));

        yield return Rule(new(
            "Lowercase variant of a reserved word is allowed as a name",
            """
            MODEL model AT "http://example.com" VERSION "1.0.0" =
            END model.
            """,
            ExpectedLog: null,
            RefHB: "3.2.7-1",
            Expected: new ModelDef
            {
                Name = "model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(1, 4, 1, 9) },
                Imports = { { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) } },
                URI = "http://example.com",
                Version = "1.0.0",
            }));

        yield return Rule(new(
            "Reserved word CLASS rejected as a name",
            """
            MODEL CLASS AT "http://example.com" VERSION "1.0.0" =
            END CLASS.
            """,
            ExpectedLog: ["Compile error at line 1:6 mismatched input 'CLASS' expecting IDENTIFIER."],
            RefHB: "3.2.7-3",
            Expected: null));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        {fragment}
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(IdentifierTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadModelDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitModelDef(p.modelDef()));
    }
}
