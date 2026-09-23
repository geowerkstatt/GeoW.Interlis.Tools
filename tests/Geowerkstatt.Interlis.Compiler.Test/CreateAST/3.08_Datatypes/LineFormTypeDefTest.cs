using Geowerkstatt.Interlis.Compiler.AST;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class LineFormTypeDefTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Custom line form definition",
            "LINE FORM CustomForm : LineStruct;",
            Description: """
                The comparison diverges: a custom LINE FORM is only accepted by ili2c when its structure conforms to
                ili2c's predefined line-geometry model (which this compiler does not model yet). The AST is verified by
                the rule-level ReadLineFormTypeDef test; the comparison is left red as a known compiler-gap signal.
                """,
            RefHB: "3.8.12.2-30",
            Expected: new List<LineFormTypeDef>
            {
                new LineFormTypeDef
                {
                    Name = "CustomForm",
                    NameLocations = { new RangePosition(0, 10, 0, 20) },
                    Structure = new Reference<ClassDef> { Path = { new("LineStruct") } },
                },
            }));

        yield return FullFile(new(
            "Custom line form referencing a defined structure",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE BezierSegment EXTENDS INTERLIS.LineSegment =
                    ControlX: 0.000 .. 100.000;
                END BezierSegment;
                LINE FORM Bezier : BezierSegment;
            END Model.
            """,
            Description: """
                Besides the name, the structure describing the curve segment must be given — always an extension of
                INTERLIS.LineSegment (RefHB 3.8.12.3-4). A custom LINE FORM whose structure is defined in the same
                model resolves cleanly (full file feeds the comparison).
                """,
            RefHB: "3.8.12.3-1",
            Ili2cDivergenceReason: "ili2c requires the model to be CONTRACTED to define line forms — a leftover INTERLIS 2.3 contract rule; RefHB 3.5.1-12 states CONTRACTED has no function anymore and is kept only for compatibility, so we accept line form definitions in any model.",
            AssertOutput: false));

        yield return Rule(new(
            "Multiple line form definitions",
            "LINE FORM FormA : StructA; FormB : StructB;",
            Description: """
                LineFormTypeDef = 'LINE' 'FORM' { LineFormType-Name ':' LineStructure-Name ';' }. The repetition allows
                several form definitions after a single 'LINE FORM'.
                """,
            RefHB: "3.8.12.3-3",
            Expected: new List<LineFormTypeDef>
            {
                new LineFormTypeDef
                {
                    Name = "FormA",
                    NameLocations = { new RangePosition(0, 10, 0, 15) },
                    Structure = new Reference<ClassDef> { Path = { new("StructA") } },
                },
                new LineFormTypeDef
                {
                    Name = "FormB",
                    NameLocations = { new RangePosition(0, 27, 0, 32) },
                    Structure = new Reference<ClassDef> { Path = { new("StructB") } },
                },
            }));

        yield return Rule(new(
            "Empty line form definition",
            "LINE FORM",
            Description: """
                The '{ ... }' repetition admits zero definitions, so a bare 'LINE FORM' is valid and yields an empty
                list.
                """,
            RefHB: "3.8.12.3-3",
            Expected: new List<LineFormTypeDef>()));

        yield return FullFile(new(
            "Multiple line forms referencing defined structures",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE StructA EXTENDS INTERLIS.LineSegment =
                    A: 0.000 .. 100.000;
                END StructA;
                STRUCTURE StructB EXTENDS INTERLIS.LineSegment =
                    B: 0.000 .. 100.000;
                END StructB;
                LINE FORM FormA : StructA;
                FormB : StructB;
            END Model.
            """,
            Description: """
                Multiple custom line forms, each bound to a line structure (an extension of INTERLIS.LineSegment,
                RefHB 3.8.12.3-4) defined in the same model (full file feeds the comparison and the full-file parse
                path).
                """,
            RefHB: "3.8.12.3-3",
            Ili2cDivergenceReason: "ili2c requires the model to be CONTRACTED to define line forms — a leftover INTERLIS 2.3 contract rule; RefHB 3.5.1-12 states CONTRACTED has no function anymore and is kept only for compatibility, so we accept line form definitions in any model.",
            AssertOutput: false));

        yield return FullFile(new(
            "Line structures extending LineSegment directly and transitively",
            """
            INTERLIS 2.4;
            CONTRACTED MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Direct EXTENDS INTERLIS.LineSegment =
                    A: 0.000 .. 100.000;
                END Direct;
                STRUCTURE Middle (ABSTRACT) EXTENDS INTERLIS.LineSegment =
                END Middle;
                STRUCTURE Transitive EXTENDS Middle =
                    B: 0.000 .. 100.000;
                END Transitive;
                LINE FORM FormA : Direct;
                FormB : Transitive;
            END Model.
            """,
            Description: """
                RefHB 3.8.12.3-4 demands an extension of INTERLIS.LineSegment; an extension of an intermediate
                structure qualifies too. The model is CONTRACTED so ili2c accepts the line form definitions and
                the comparison exercises the structure rule on both sides.
                """,
            RefHB: "3.8.12.3-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Line structure not extending LineSegment",
            """
            INTERLIS 2.4;
            CONTRACTED MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Standalone =
                    A: 0.000 .. 100.000;
                END Standalone;
                LINE FORM Custom : Standalone;
            END Model.
            """,
            Description: """
                The structure describing a curve segment must always be an extension of the predefined structure
                INTERLIS.LineSegment, which carries the segment end point every curve form shares.
                """,
            RefHB: "3.8.12.3-4",
            ExpectedLog: ["Type check error in 'Model.Custom' at 6:14-6:33: the line structure 'Standalone' must be an extension of the predefined structure INTERLIS.LineSegment."],
            Ili2cDivergenceReason: """
                ili2c does not enforce the line-structure rule and accepts any structure as a line form (the model
                is CONTRACTED, so its contract rule does not interfere); RefHB 3.8.12.3-4 states a line structure
                must always be an extension of INTERLIS.LineSegment, so we reject.
                """,
            AssertOutput: false));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            {fragment}
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(LineFormTypeDefTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadLineFormTypeDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitLineFormTypeDef(p.lineFormTypeDef()));
    }
}
