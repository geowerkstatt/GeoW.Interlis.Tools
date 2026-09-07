using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// Verifies that the AST builder (<see cref="CreateAST.Interlis24Visitor"/>) is resilient to syntactically
/// incomplete input, as produced while a model is being typed in an editor. ANTLR error recovery leaves labeled
/// tokens and subrules null (or as synthetic "missing" tokens), so a naive visitor would throw
/// <see cref="System.NullReferenceException"/> / <see cref="System.FormatException"/> / index errors and abort the
/// whole compilation instead of reporting the syntax error. Each case therefore compiles a half-typed construct
/// through the full pipeline and asserts that it produces the expected diagnostics <em>without throwing</em>
/// (a thrown exception fails <see cref="TestTools.AssertReadFile"/>). Only the diagnostics are asserted
/// (<c>AssertOutput: false</c>): the point is that the compiler stays alive and keeps reporting, not the exact shape
/// of the partial AST.
/// </summary>
public class ResilienceTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return FullFile(new(
            "Topic without a name",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC
            """,
            Description: "Definitions whose mandatory name is not yet typed: the definition is dropped, the model survives.",
            ExpectedLog: ["Compile error at line 3:5 mismatched input '<EOF>' expecting IDENTIFIER."],
            AssertOutput: false));

        yield return FullFile(new(
            "Class without a name",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS
            """,
            Description: "Definitions whose mandatory name is not yet typed: the definition is dropped, the model survives.",
            ExpectedLog: ["Compile error at line 4:5 mismatched input '<EOF>' expecting IDENTIFIER."],
            AssertOutput: false));

        yield return FullFile(new(
            "View without a name",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            VIEW
            """,
            Description: "Definitions whose mandatory name is not yet typed: the definition is dropped, the model survives.",
            ExpectedLog: ["Compile error at line 4:4 mismatched input '<EOF>' expecting IDENTIFIER."],
            AssertOutput: false));

        yield return FullFile(new(
            "Graphic without a name",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            GRAPHIC
            """,
            Description: "Definitions whose mandatory name is not yet typed: the definition is dropped, the model survives.",
            ExpectedLog: ["Compile error at line 4:7 mismatched input '<EOF>' expecting IDENTIFIER."],
            AssertOutput: false));

        yield return FullFile(new(
            "Function without a name",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            FUNCTION
            """,
            Description: "Definitions whose mandatory name is not yet typed: the definition is dropped, the model survives.",
            ExpectedLog: ["Compile error at line 3:8 mismatched input '<EOF>' expecting IDENTIFIER."],
            AssertOutput: false));

        yield return FullFile(new(
            "Function without a return type",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            FUNCTION f()
            """,
            Description: "Definitions whose mandatory name is not yet typed: the definition is dropped, the model survives.",
            ExpectedLog: ["Compile error at line 3:12 mismatched input '<EOF>' expecting ':'."],
            AssertOutput: false));

        yield return FullFile(new(
            "Metadata basket without a name",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            SIGN BASKET
            """,
            Description: "Definitions whose mandatory name is not yet typed: the definition is dropped, the model survives.",
            ExpectedLog: ["Compile error at line 3:11 mismatched input '<EOF>' expecting IDENTIFIER."],
            AssertOutput: false));

        yield return FullFile(new(
            "Attribute name after SUBDIVISION",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS C =
            SUBDIVISION
            """,
            Description: "Attribute name absent although a CONTINUOUS/SUBDIVISION prefix already committed the attribute rule.",
            ExpectedLog: ["Compile error at line 5:11 mismatched input '<EOF>' expecting IDENTIFIER."],
            AssertOutput: false));

        yield return FullFile(new(
            "Reference attribute without a target",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS C =
            a : REFERENCE TO
            """,
            Description: "A reference attribute whose target is not yet written.",
            ExpectedLog: ["Compile error at line 5:16 mismatched input '<EOF>' expecting {'(', 'ANYCLASS', 'ANYSTRUCTURE', 'HALIGNMENT', 'INTERLIS', 'METAOBJECT', 'NAME', 'REFSYSTEM', 'SIGN', 'URI', 'VALIGNMENT', IDENTIFIER}."],
            AssertOutput: false));

        yield return FullFile(new(
            "Mandatory constraint without a condition",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS C =
            MANDATORY CONSTRAINT c:
            """,
            Description: """
                Constraints whose mandatory condition expression is not yet written: the constraint is dropped so it
                never reaches the type checker with a null condition.
                """,
            ExpectedLog:
            [
                "Compile error at line 5:23 mismatched input '<EOF>' expecting {'(', '>', '>>', '#', 'AGGREGATES', 'AREA', 'DEFINED', 'HALIGNMENT', 'INSPECTION', 'INTERLIS', 'LNBASE', 'METAOBJECT', 'NAME', 'NOT', 'PARAMETER', 'PARENT', 'PI', 'REFSYSTEM', 'SIGN', 'THATAREA', 'THIS', 'THISAREA', 'UNDEFINED', 'URI', 'VALIGNMENT', EXP_NUMBER, DECIMAL_NUMBER, SIGNED_NUMBER, POS_NUMBER, IDENTIFIER, DOUBLE_QUOTE_OPEN, '\\\\'}.",
                "Rule 'expression' at line 5:23 not implemented.",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Set constraint without a condition",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS C =
            SET CONSTRAINT WHERE a :
            """,
            Description: """
                Constraints whose mandatory condition expression is not yet written: the constraint is dropped so it
                never reaches the type checker with a null condition.
                """,
            ExpectedLog:
            [
                "Compile error at line 5:24 mismatched input '<EOF>' expecting {'(', '>', '>>', '#', 'AGGREGATES', 'AREA', 'DEFINED', 'HALIGNMENT', 'INSPECTION', 'INTERLIS', 'LNBASE', 'METAOBJECT', 'NAME', 'NOT', 'PARAMETER', 'PARENT', 'PI', 'REFSYSTEM', 'SIGN', 'THATAREA', 'THIS', 'THISAREA', 'UNDEFINED', 'URI', 'VALIGNMENT', EXP_NUMBER, DECIMAL_NUMBER, SIGNED_NUMBER, POS_NUMBER, IDENTIFIER, DOUBLE_QUOTE_OPEN, '\\\\'}.",
                "Rule 'expression' at line 5:24 not implemented.",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Unique constraint without elements",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS C =
            UNIQUE ;
            """,
            Description: """
                Constraints whose mandatory condition expression is not yet written: the constraint is dropped so it
                never reaches the type checker with a null condition.
                """,
            ExpectedLog: ["Compile error at line 5:7 mismatched input ';' expecting {'(', 'AGGREGATES', 'PARENT', 'THATAREA', 'THIS', 'THISAREA', 'WHERE', IDENTIFIER, '\\\\'}."],
            AssertOutput: false));

        yield return FullFile(new(
            "Text type with '*' but no length",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            DOMAIN D = TEXT *;
            """,
            Description: "Numeric tokens that are absent or synthetic (would otherwise fail int.Parse).",
            ExpectedLog:
            [
                "Compile error at line 3:17 missing POS_NUMBER at ';'.",
                "Compile error at line 3:18 extraneous input '<EOF>' expecting {'CLASS', 'CONTEXT', 'DOMAIN', 'END', 'FUNCTION', 'LINE', 'PARAMETER', 'REFSYSTEM', 'SIGN', 'STRUCTURE', 'TOPIC', 'UNIT', 'VIEW'}.",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Coordinate rotation without axes",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            DOMAIN D = COORD 0.0 .. 1.0, 0.0 .. 1.0, ROTATION ->
            """,
            Description: "Numeric tokens that are absent or synthetic (would otherwise fail int.Parse).",
            ExpectedLog:
            [
                "Compile error at line 3:50 missing POS_NUMBER at '->'.",
                "Compile error at line 3:52 mismatched input '<EOF>' expecting POS_NUMBER.",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Numeric reference system left open",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            DOMAIN D = NUMERIC {
            """,
            Description: "Numeric tokens that are absent or synthetic (would otherwise fail int.Parse).",
            ExpectedLog:
            [
                "Compile error at line 3:20 mismatched input '<EOF>' expecting {'HALIGNMENT', 'INTERLIS', 'METAOBJECT', 'NAME', 'REFSYSTEM', 'SIGN', 'URI', 'VALIGNMENT', IDENTIFIER}.",
                "Type check error in 'M.D': must be declared ABSTRACT because its type is not fully defined.",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "View projection without a source",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            VIEW V PROJECTION
            """,
            Description: "A view formation and a signature assignment whose operands are not yet written.",
            ExpectedLog: ["Compile error at line 4:17 mismatched input '<EOF>' expecting 'OF'."],
            AssertOutput: false));

        yield return FullFile(new(
            "Signature assignment without a value",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            GRAPHIC G =
            r : ( p := )
            """,
            Description: "A view formation and a signature assignment whose operands are not yet written.",
            ExpectedLog:
            [
                "Compile error at line 5:11 mismatched input ')' expecting {'{', '>', '>>', '#', 'ACCORDING', 'AGGREGATES', 'AREA', 'HALIGNMENT', 'INSPECTION', 'INTERLIS', 'LNBASE', 'METAOBJECT', 'NAME', 'PARAMETER', 'PARENT', 'PI', 'REFSYSTEM', 'SIGN', 'THATAREA', 'THIS', 'THISAREA', 'UNDEFINED', 'URI', 'VALIGNMENT', EXP_NUMBER, DECIMAL_NUMBER, SIGNED_NUMBER, POS_NUMBER, IDENTIFIER, DOUBLE_QUOTE_OPEN, '\\\\'}.",
                "Compile error at line 5:12 mismatched input '<EOF>' expecting {';', ','}.",
                "Type check error in 'M.T.G': must be BASED ON a class or view, or EXTEND a graphic to inherit its base.",
                "Type check error in 'M.T.G': the drawing rule 'r' must specify the class of the graphic signatures it assigns ('OF ...').",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Path with a trailing arrow",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS C =
            MANDATORY CONSTRAINT c: a ->
            """,
            Description: "An object path with a trailing '->' (the following step is not yet written).",
            ExpectedLog:
            [
                "Compile error at line 5:28 mismatched input '<EOF>' expecting {'AGGREGATES', 'PARENT', 'THATAREA', 'THIS', 'THISAREA', IDENTIFIER, '\\\\'}.",
                // The head 'a' is completely typed and names no member of C, so the path resolver reports it even
                // though the path's tail is incomplete (only the empty trailing element is exempt from the check).
                "Could not resolve 'a' in 'M.T.C'",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Derived unit missing a factor",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            UNIT u = 5 *
            """,
            Description: "A derived unit whose conversion factor is not yet written.",
            ExpectedLog: ["Compile error at line 3:12 mismatched input '<EOF>' expecting {'LNBASE', 'PI', EXP_NUMBER, DECIMAL_NUMBER, SIGNED_NUMBER, POS_NUMBER}."],
            AssertOutput: false));

        yield return FullFile(new(
            "Imports without a model name",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            IMPORTS
            """,
            Description: "An import whose model name is not yet written.",
            ExpectedLog: ["Compile error at line 3:7 mismatched input '<EOF>' expecting {'INTERLIS', 'UNQUALIFIED', IDENTIFIER}."],
            AssertOutput: false));

        yield return FullFile(new(
            "Association cardinality left open",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            ASSOCIATION A =
            r -- X;
            CARDINALITY =
            """,
            Description: "An association whose CARDINALITY clause has no bounds yet (the whole association still compiles).",
            ExpectedLog:
            [
                "Compile error at line 6:13 mismatched input '<EOF>' expecting '{'.",
                "Compile error at line 4:0 Start name 'A' and end name '' do not match.",
                "Could not resolve 'reference 'X' from M.T.A'",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Enumeration with a trailing comma",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            DOMAIN D = (red,
            """,
            Description: "An enumeration with a trailing ',' whose next value is not written yet.",
            ExpectedLog: ["Compile error at line 3:16 mismatched input '<EOF>' expecting IDENTIFIER."],
            AssertOutput: false));

        yield return FullFile(new(
            "Existence constraint without a path",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS C =
            EXISTENCE CONSTRAINT
            """,
            Description: "An existence constraint whose checked path is not yet written.",
            ExpectedLog: ["Compile error at line 5:20 mismatched input '<EOF>' expecting {'AGGREGATES', 'PARENT', 'THATAREA', 'THIS', 'THISAREA', IDENTIFIER, '\\\\'}."],
            AssertOutput: false));

        yield return FullFile(new(
            "Runtime parameter used as a factor",
            """
            INTERLIS 2.4;
            MODEL M AT "urn:example" VERSION "1" =
            TOPIC T =
            CLASS C =
            MANDATORY CONSTRAINT k: PARAMETER
            """,
            Description: "A 'PARAMETER' runtime-parameter reference used as an expression factor, left incomplete.",
            ExpectedLog: ["Compile error at line 5:33 mismatched input '<EOF>' expecting {'HALIGNMENT', 'INTERLIS', 'METAOBJECT', 'NAME', 'REFSYSTEM', 'SIGN', 'URI', 'VALIGNMENT', IDENTIFIER}."],
            AssertOutput: false));
    }

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(ResilienceTest), GetCases());

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }
}
