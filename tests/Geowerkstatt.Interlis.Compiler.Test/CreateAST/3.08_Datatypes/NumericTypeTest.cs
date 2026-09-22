using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class NumericTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Abstract numeric",
            "NUMERIC",
            RefHB: "3.8.5-1",
            Expected: new NumericType { SourceRange = new RangePosition(0, 0, 0, 7) }));

        yield return Rule(new(
            "Abstract numeric with unit",
            "NUMERIC [INTERLIS.LENGTH]",
            RefHB: "3.8.5-6",
            Expected: new NumericType
            {
                Unit = new Reference<UnitDef> { Path = { new("INTERLIS"), new("LENGTH") } },
                SourceRange = new RangePosition(0, 0, 0, 25),
            }));

        yield return Rule(new(
            "Numeric range",
            "000..999",
            RefHB: "3.8.5-1",
            Expected: new DecimalType { Min = 0, Max = 999, Precision = 0, SourceRange = new RangePosition(0, 0, 0, 8) }));

        yield return Rule(new(
            "Numeric range with positive signed bounds",
            "+5 .. +10",
            RefHB: "3.2.4-3",
            Expected: new DecimalType { Min = 5, Max = 10, Precision = 0, SourceRange = new RangePosition(0, 0, 0, 9) }));

        yield return Rule(new(
            "Numeric range with negative signed bounds",
            "-10 .. -5",
            RefHB: "3.2.4-3",
            Expected: new DecimalType { Min = -10, Max = -5, Precision = 0, SourceRange = new RangePosition(0, 0, 0, 9) }));

        yield return Rule(new(
            "Numeric range with uppercase E scaling",
            "0.10E-2 .. 0.20E-1",
            RefHB: "3.2.4-3",
            Expected: new FloatType { MantissaLength = 2, Min = 0.001, Max = 0.02, SourceRange = new RangePosition(0, 0, 0, 18) }));

        yield return Rule(new(
            "Circular numeric",
            "000..999 CIRCULAR",
            RefHB: "3.8.5-1",
            Expected: new DecimalType { Min = 0, Max = 999, Precision = 0, Circular = true, SourceRange = new RangePosition(0, 0, 0, 17) }));

        yield return Rule(new(
            "Circular numeric with unit",
            "0.00 .. 359.99 CIRCULAR [INTERLIS.m]",
            RefHB: "3.8.5-2",
            Expected: new DecimalType
            {
                Min = 0,
                Max = 359.99,
                Precision = -2,
                Circular = true,
                Unit = new Reference<UnitDef> { Path = { new("INTERLIS"), new("m") } },
                SourceRange = new RangePosition(0, 0, 0, 36),
            }));

        yield return Rule(new(
            "Numeric with minimum greater than maximum",
            "999 .. 000",
            ExpectedLog: ["Compile error at 1:0-1:3 Number minimum <999> must be smaller than maximum <0>."],
            RefHB: "3.8.5-1",
            Expected: new DecimalType { Min = 0, Max = 999, Precision = 0, SourceRange = new RangePosition(0, 0, 0, 10) }));

        yield return Rule(new(
            "Float range with reversed scalings is rejected",
            "0.1E10 .. 0.1E7",
            Description: """
                The handbook orders the scalings (minimum scaling smaller than maximum scaling). A REVERSED order is
                caught here through the values: with equal Stellenzahl, a larger minimum scaling makes the minimum value
                larger. Only EQUAL scalings are deliberately tolerated — they express an unambiguous fixed grid, the
                ordering is a canonical-form rule, it is undefined for a zero minimum, and ili2c does not enforce it.
                """,
            ExpectedLog: ["Compile error at 1:0-1:6 Number minimum <1000000000> must be smaller than maximum <1000000>."],
            RefHB: "3.8.5-3",
            Expected: new FloatType { MantissaLength = 1, Min = 1000000, Max = 1000000000, SourceRange = new RangePosition(0, 0, 0, 15) }));

        yield return Rule(new(
            "Numeric with unit",
            "0 .. 100 [INTERLIS.m]",
            RefHB: "3.8.5-1",
            Expected: new DecimalType
            {
                Min = 0,
                Max = 100,
                Precision = 0,
                Unit = new Reference<UnitDef> { Path = { new("INTERLIS"), new("m") } },
                SourceRange = new RangePosition(0, 0, 0, 21),
            }));

        yield return Rule(new(
            "Exponential numeric",
            "0.10e-2 .. 0.20e-1",
            RefHB: "3.8.5-3",
            Expected: new FloatType { MantissaLength = 2, Min = 0.001, Max = 0.02, SourceRange = new RangePosition(0, 0, 0, 18) }));

        yield return Rule(new(
            "Numeric with precision",
            "-1.50 .. 10.00",
            RefHB: "3.8.5-3",
            Expected: new DecimalType { Min = -1.5, Max = 10, Precision = -2, SourceRange = new RangePosition(0, 0, 0, 14) }));

        yield return Rule(new(
            "Numeric with different precision",
            "0.00 .. 10.0",
            ExpectedLog: ["Compile error at 1:0-1:4 Number minimum and maximum must have the same precision but minimum has precision <0.01> and maximum has precision <0.1>."],
            RefHB: "3.8.5-3",
            Expected: new DecimalType { Min = 0, Max = 10, Precision = -2, SourceRange = new RangePosition(0, 0, 0, 12) }));

        yield return Rule(new(
            "Numeric with mixed exponential and decimal",
            "0.100e-2 .. 1.000",
            ExpectedLog: ["Compile error at 1:0-1:8 Number minimum and maximum must both use mantissa (exponential) notation or both use decimal notation."],
            RefHB: "3.8.5-3",
            Expected: new FloatType { MantissaLength = 3, Min = 0.001, Max = 1, SourceRange = new RangePosition(0, 0, 0, 17) },
            Ili2cDivergenceReason: "ili2c accepts a range mixing mantissa and decimal notation (it compares only the digit counts); RefHB 3.8.5-3 requires both bounds in mantissa notation for float ranges, so we reject the mix."));

        yield return Rule(new(
            "Numeric with mixed decimal and exponential",
            "0.001 .. 0.100e1",
            Description: """
                The reverse mixing direction (decimal minimum, mantissa maximum); the Stellenzahl matches deliberately so
                only the notation mixing is reported.
                """,
            ExpectedLog: ["Compile error at 1:0-1:5 Number minimum and maximum must both use mantissa (exponential) notation or both use decimal notation."],
            RefHB: "3.8.5-3",
            Expected: new DecimalType { Min = 0.001, Max = 1, Precision = -3, SourceRange = new RangePosition(0, 0, 0, 16) },
            Ili2cDivergenceReason: "ili2c accepts a range mixing mantissa and decimal notation (it compares only the digit counts); RefHB 3.8.5-3 requires both bounds in mantissa notation for float ranges, so we reject the mix."));

        yield return Rule(new(
            "Numeric with mixed integer and exponential",
            "0 .. 0.1e3",
            Description: """
                An integer bound mixed with a mantissa bound violates both rules: the Stellenzahl differs (0 vs 1 digits)
                and the notations are mixed.
                """,
            ExpectedLog: [
                "Compile error at 1:0-1:1 Number minimum and maximum must have the same precision but minimum has precision <1> and maximum has precision <0.1>.",
                "Compile error at 1:0-1:1 Number minimum and maximum must both use mantissa (exponential) notation or both use decimal notation.",
            ],
            RefHB: "3.8.5-3",
            Expected: new DecimalType { Min = 0, Max = 100, Precision = 0, SourceRange = new RangePosition(0, 0, 0, 10) }));

        yield return Rule(new(
            "Numeric inappropriate for double",
            "0.00000 .. 100000000000000.00000",
            ExpectedLog: ["Compile error at 1:0-1:7 The given range <0 .. 100000000000000> with a precision of <1E-05> cannot be represented by a double precision floating point number."],
            RefHB: "3.8.5-3",
            Expected: new DecimalType { Min = 0, Max = 100000000000000, Precision = -5, SourceRange = new RangePosition(0, 0, 0, 32) },
            Ili2cDivergenceReason: "we reject ranges not representable as a double, ili2c accepts them"));

        yield return Rule(new(
            "Numeric appropriate for double",
            "0.0 .. 100000000000000.0",
            RefHB: "3.8.5-3",
            Expected: new DecimalType { Min = 0, Max = 100000000000000, Precision = -1, SourceRange = new RangePosition(0, 0, 0, 24) }));

        yield return Rule(new(
            "Exponential numeric inappropriate for double",
            "0.10000000000000000E1 .. 0.20000000000000000E3",
            Description: """
                In mantissa notation the step is relative to each bound's own magnitude, so representability depends on
                the mantissa digit count alone (not on the scaling): 17 significant digits exceed a double's ~16
                decimal digits at every magnitude.
                """,
            ExpectedLog: ["Compile error at 1:0-1:21 The given range <1 .. 200> with a precision of <1E-17> cannot be represented by a double precision floating point number."],
            RefHB: "3.8.5-3",
            Expected: new FloatType { MantissaLength = 17, Min = 1, Max = 200, SourceRange = new RangePosition(0, 0, 0, 46) },
            Ili2cDivergenceReason: "we reject ranges not representable as a double, ili2c accepts them"));

        yield return Rule(new(
            "Exponential numeric appropriate for double",
            "0.100000000000000E1 .. 0.200000000000000E3",
            RefHB: "3.8.5-3",
            Expected: new FloatType { MantissaLength = 15, Min = 1, Max = 200, SourceRange = new RangePosition(0, 0, 0, 42) }));

        yield return Rule(new(
            "Float range with differing mantissa digit counts is rejected",
            "0.1E7 .. 0.1000E10",
            Description: """
                The Stellenzahl of the minimum and the maximum must match. In mantissa notation the digits of the
                mantissa count, so a differing scaling can not compensate the difference.
                """,
            ExpectedLog: ["Compile error at 1:0-1:5 Number minimum and maximum must have the same precision but minimum has precision <0.1> and maximum has precision <0.0001>."],
            RefHB: "3.8.5-3",
            Expected: new FloatType { MantissaLength = 1, Min = 1000000, Max = 1000000000, SourceRange = new RangePosition(0, 0, 0, 18) }));

        yield return Rule(new(
            "Clockwise numeric",
            "0 .. 100 CLOCKWISE",
            RefHB: "3.8.5-1",
            Expected: new DecimalType { Min = 0, Max = 100, Precision = 0, Orientation = NumericType.AngleOrientation.Clockwise, SourceRange = new RangePosition(0, 0, 0, 18) }));

        yield return Rule(new(
            "Counterclockwise numeric",
            "0 .. 100 COUNTERCLOCKWISE",
            RefHB: "3.8.5-1",
            Expected: new DecimalType { Min = 0, Max = 100, Precision = 0, Orientation = NumericType.AngleOrientation.CounterClockwise, SourceRange = new RangePosition(0, 0, 0, 25) }));

        yield return Rule(new(
            "Circular numeric with unit and clockwise orientation",
            "0.00 .. 359.99 CIRCULAR [INTERLIS.m] CLOCKWISE",
            RefHB: "3.8.5-18",
            Expected: new DecimalType
            {
                Min = 0,
                Max = 359.99,
                Precision = -2,
                Circular = true,
                Unit = new Reference<UnitDef> { Path = { new("INTERLIS"), new("m") } },
                Orientation = NumericType.AngleOrientation.Clockwise,
                SourceRange = new RangePosition(0, 0, 0, 46),
            }));

        yield return Rule(new(
            "Circular numeric with unit and counterclockwise orientation",
            "0.00 .. 359.99 CIRCULAR [INTERLIS.m] COUNTERCLOCKWISE",
            RefHB: "3.8.5-18",
            Expected: new DecimalType
            {
                Min = 0,
                Max = 359.99,
                Precision = -2,
                Circular = true,
                Unit = new Reference<UnitDef> { Path = { new("INTERLIS"), new("m") } },
                Orientation = NumericType.AngleOrientation.CounterClockwise,
                SourceRange = new RangePosition(0, 0, 0, 53),
            }));

        yield return Rule(new(
            "Numeric with coordinate-domain reference system",
            "0 .. 100 <Refsys.CoordDomain>",
            RefHB: "3.8.5-19",
            Expected: new DecimalType
            {
                Min = 0,
                Max = 100,
                Precision = 0,
                RefSystem = new RefSys
                {
                    Value = new RefSys.CoordDomainRef
                    {
                        Domain = new Reference<DomainDef> { Path = { new("Refsys"), new("CoordDomain") } },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 29),
            }));

        yield return Rule(new(
            "Numeric with coordinate-domain reference system and axis",
            "0 .. 100 <Refsys.CoordDomain[1]>",
            RefHB: "3.8.5-19",
            Expected: new DecimalType
            {
                Min = 0,
                Max = 100,
                Precision = 0,
                RefSystem = new RefSys
                {
                    Value = new RefSys.CoordDomainRef
                    {
                        Domain = new Reference<DomainDef> { Path = { new("Refsys"), new("CoordDomain") } },
                    },
                    Axis = 1,
                },
                SourceRange = new RangePosition(0, 0, 0, 32),
            }));

        yield return Rule(new(
            "Numeric with meta-object reference system",
            "0 .. 100 {Refsys.MetaObj}",
            RefHB: "3.8.5-19",
            Expected: new DecimalType
            {
                Min = 0,
                Max = 100,
                Precision = 0,
                RefSystem = new RefSys
                {
                    Value = new RefSys.MetaObjectRef
                    {
                        Basket = new Reference<MetaDataBasketDef> { Path = { new("Refsys") } },
                        MetaObject = new Reference<MetaObjectDeclaration> { Path = { new("MetaObj") } },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 25),
            }));

        yield return Rule(new(
            "Numeric with meta-object reference system and axis",
            "0 .. 100 {Refsys.MetaObj[2]}",
            RefHB: "3.8.5-19",
            Expected: new DecimalType
            {
                Min = 0,
                Max = 100,
                Precision = 0,
                RefSystem = new RefSys
                {
                    Value = new RefSys.MetaObjectRef
                    {
                        Basket = new Reference<MetaDataBasketDef> { Path = { new("Refsys") } },
                        MetaObject = new Reference<MetaObjectDeclaration> { Path = { new("MetaObj") } },
                    },
                    Axis = 2,
                },
                SourceRange = new RangePosition(0, 0, 0, 28),
            }));

        yield return FullFile(new(
            "Extension range narrowing and precision example",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Normal = 0.00 .. 7.99;
                    Genau EXTENDS Normal = 0.0000 .. 7.9949;
                    Eingeschraenkt EXTENDS Normal = 1.00 .. 6.99;
            END Model.
            """,
            Description: """
                Worked example RefHB 3.8.5-4/-5: in extensions the range may only be narrowed and the precision
                (Stellenzahl) may not be changed. The example itself marks Genau as invalid ("falsch, da Stellenzahl
                grösser"); Eingeschraenkt narrows the range at the same precision and is valid.
                """,
            ExpectedLog: ["Type check error in 'Model.Genau' at 5:8-5:48: the precision must match the inherited precision."],
            RefHB: "3.8.5-5",
            Ili2cDivergenceReason: "ili2c does not enforce the precision rule and accepts Genau although RefHB 3.8.5-5 itself marks it as invalid (falsch, da Stellenzahl grösser); we reject per RefHB 3.8.5-4.",
            AssertOutput: false));

        yield return FullFile(new(
            "Extension unit compatibility example",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    foot [ft] = 0.3048 [INTERLIS.m];
                DOMAIN
                    Distanz (ABSTRACT) = NUMERIC [INTERLIS.LENGTH];
                    MeterDist (ABSTRACT) EXTENDS Distanz = NUMERIC [INTERLIS.m];
                    FussDist (ABSTRACT) EXTENDS Distanz = NUMERIC [ft];
                    KurzeMeter EXTENDS MeterDist = 0.00 .. 100.00 [INTERLIS.m];
                    KurzeFuesse EXTENDS FussDist = 0.00 .. 100.00 [ft];
                    KurzeFuesse2 (ABSTRACT) EXTENDS KurzeMeter = NUMERIC [ft];
            END Model.
            """,
            Description: """
                Worked example: the unit of an extension must be an extension of (or compatible with) the base domain's
                unit. The derived 'foot' (defined from INTERLIS.m, RefHB 3.9.2-7) counts as an extension of LENGTH, so
                FussDist is legal; the last line is doubly wrong — a bound-less NUMERIC (abstract, RefHB 3.8.5-1) can
                not extend the concrete range it inherits from KurzeMeter, and its [ft] overrides the inherited concrete
                unit m (RefHB 3.8.5-10).
                """,
            ExpectedLog:
            [
                "Type check error in 'Model.KurzeFuesse2' at 11:8-11:66: an abstract NUMERIC can not extend a concrete numeric range.",
                "Type check error in 'Model.KurzeFuesse2' at 11:8-11:66: the inherited concrete unit 'm' can not be overridden.",
            ],
            RefHB: "3.8.5-13",
            AssertOutput: false));

        // The refSys forms ('<Coord>' / '{Frame}') reference a reference system that must be defined;
        // they are exercised in a full reference-system context in the 3.10.3 tests, where ili2c also
        // accepts them. Standalone they diverge from ili2c (unresolved reference system).
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
        => ComparisonRows(nameof(NumericTypeTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadNumericType(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitNumericType(p.numericType()));
    }
}
