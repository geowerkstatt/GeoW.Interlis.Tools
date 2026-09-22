using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class UnitTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Abstract base unit",
            "Length (ABSTRACT);",
            RefHB: "3.9.1-1",
            Expected: new UnitDef
            {
                Name = "Length",
                Term = "Length",
                NameLocations = { new RangePosition(0, 0, 0, 6) },
                Properties = { Property.Abstract },
            }));

        yield return Rule(new(
            "Abstract unit with abbreviation",
            "Length [m] (ABSTRACT);",
            ExpectedLog: ["Compile error at 1:11-1:12 mismatched input '(' expecting {';', '=', 'EXTENDS'}."],
            RefHB: "3.9.1-1",
            Expected: new UnitDef
            {
                Name = "m",
                Term = "Length",
                NameLocations = { new RangePosition(0, 8, 0, 9) },
                Properties = { Property.Abstract },
            }));

        yield return FullFile(new(
            "Extended specific unit",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                UNIT
                    Meter [m];
                    CoolMeter [x] EXTENDS m;
            END ModelName.
            """,
            ExpectedLog: ["Type check error in 'ModelName.x' at 5:8-5:32: can not extend 'ModelName.m' because it is not ABSTRACT."],
            RefHB: "3.9.1-1",
            AssertOutput: false));

        yield return Rule(new(
            "Extending base unit",
            "Meter [m] EXTENDS Length;",
            RefHB: "3.9.1-3",
            Expected: new UnitDef
            {
                Name = "m",
                Term = "Meter",
                NameLocations = { new RangePosition(0, 7, 0, 8) },
                Extends = new Reference<UnitDef> { Path = { new("Length") } },
            }));

        yield return Rule(new(
            "Unit with documentation",
            """
            /** Base unit for all temperatures. */
            !!@ meta=value
            Temperature (ABSTRACT);
            """,
            RefHB: "3.9.1-3",
            Ech0117: "5-22",
            Expected: new UnitDef
            {
                Name = "Temperature",
                Term = "Temperature",
                NameLocations = { new RangePosition(2, 0, 2, 11) },
                Properties = { Property.Abstract },
                DocComments = { "/** Base unit for all temperatures. */" },
                MetaAttributes = { { "meta", "value" } },
            }));

        yield return Rule(new(
            "Derived unit",
            "AngleDegree [deg] = 360 / 2 / PI [rad];",
            RefHB: "3.9.2-1",
            Expected: new UnitDef
            {
                Name = "deg",
                Term = "AngleDegree",
                NameLocations = { new RangePosition(0, 13, 0, 16) },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new ArithmeticExpression
                    {
                        Operator = ArithmeticExpression.ArithmeticOperator.Division,
                        FirstOperand = new ArithmeticExpression
                        {
                            Operator = ArithmeticExpression.ArithmeticOperator.Division,
                            FirstOperand = new NumericConstant { Value = 360 },
                            SecondOperand = new NumericConstant { Value = 2 },
                        },
                        SecondOperand = new NumericConstant { Value = NumericConstant.PredefinedConstant.Pi },
                    },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("rad") } } }
                }
            }));

        yield return Rule(new(
            "Function defined unit",
            "Celsius = FUNCTION // conv // [K];",
            RefHB: "3.9.2-1",
            Expected: new UnitDef
            {
                Name = "Celsius",
                Term = "Celsius",
                NameLocations = { new RangePosition(0, 0, 0, 7) },
                Explanation = " conv ",
                Expression = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("K") } } },
            }));

        yield return Rule(new(
            "Derived unit single constant",
            "Kilometer [km] = 1000 [m];",
            RefHB: "3.9.2-2",
            Expected: new UnitDef
            {
                Term = "Kilometer",
                Name = "km",
                NameLocations = { new RangePosition(0, 11, 0, 13) },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new NumericConstant
                    {
                        Value = 1000,
                    },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("m") } } },
                },
            }));

        yield return Rule(new(
            "Derived unit constant division",
            "Centimeter [cm] = 1 / 100 [m];",
            RefHB: "3.9.2-3",
            Expected: new UnitDef
            {
                Term = "Centimeter",
                Name = "cm",
                NameLocations = { new RangePosition(0, 12, 0, 14) },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new ArithmeticExpression
                    {
                        Operator = ArithmeticExpression.ArithmeticOperator.Division,
                        FirstOperand = new NumericConstant
                        {
                            Value = 1,
                        },
                        SecondOperand = new NumericConstant
                        {
                            Value = 100,
                        },
                    },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("m") } } },
                },
            }));

        yield return Rule(new(
            "Derived unit decimal constant with line comment",
            "Inch [in] = 0.0254 [m]; !! 1 Zoll ist 2.54 cm",
            RefHB: "3.9.2-4",
            Expected: new UnitDef
            {
                Term = "Inch",
                Name = "in",
                NameLocations = { new RangePosition(0, 6, 0, 8) },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new NumericConstant
                    {
                        Value = 0.0254,
                    },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("m") } } },
                },
            }));

        yield return Rule(new(
            "Function derived unit with short name",
            "Fahrenheit [oF] = FUNCTION // (oF + 459.67) / 1.8 // [K];",
            RefHB: "3.9.2-5",
            Expected: new UnitDef
            {
                Term = "Fahrenheit",
                Name = "oF",
                NameLocations = { new RangePosition(0, 12, 0, 14) },
                Expression = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("K") } } },
                Explanation = " (oF + 459.67) / 1.8 ",
            }));

        yield return Rule(new(
            "Abstract composed unit",
            "Area (ABSTRACT) = (Length * Length);",
            RefHB: "3.9.3-1",
            Expected: new UnitDef
            {
                Name = "Area",
                Term = "Area",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                Properties = { Property.Abstract },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Length") } } },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Length") } } }
                }
            }));

        yield return Rule(new(
            "Composed unit",
            "KilometersPerHour [kmh] EXTENDS Speed = (km / h);",
            RefHB: "3.9.3-1",
            Expected: new UnitDef
            {
                Name = "kmh",
                Term = "KilometersPerHour",
                NameLocations = { new RangePosition(0, 19, 0, 22) },
                Extends = new Reference<UnitDef> { Path = { new("Speed") } },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Division,
                    FirstOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("km") } } },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("h") } } }
                }
            }));

        yield return Rule(new(
            "Abstract composed unit with division",
            "Speed (ABSTRACT) = ( Length / Time );",
            RefHB: "3.9.3-3",
            Expected: new UnitDef
            {
                Term = "Speed",
                Name = "Speed",
                NameLocations = { new RangePosition(0, 0, 0, 5) },
                Properties = { Property.Abstract },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Division,
                    FirstOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Length") } } },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Time") } } },
                },
            }));

        yield return Rule(new(
            "Composed unit with multiple multiplications",
            "Volume (ABSTRACT) = ( Length * Length * Length );",
            RefHB: "3.9.3-7",
            Expected: new UnitDef
            {
                Term = "Volume",
                Name = "Volume",
                NameLocations = { new RangePosition(0, 0, 0, 6) },
                Properties = { Property.Abstract },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new ArithmeticExpression
                    {
                        Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                        FirstOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Length") } } },
                        SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Length") } } },
                    },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Length") } } },
                },
            }));

        yield return Rule(new(
            "Composed unit with multiple divisions",
            "Acceleration (ABSTRACT) = ( Length / Time / Time );",
            RefHB: "3.9.3-7",
            Expected: new UnitDef
            {
                Term = "Acceleration",
                Name = "Acceleration",
                NameLocations = { new RangePosition(0, 0, 0, 12) },
                Properties = { Property.Abstract },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Division,
                    FirstOperand = new ArithmeticExpression
                    {
                        Operator = ArithmeticExpression.ArithmeticOperator.Division,
                        FirstOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Length") } } },
                        SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Time") } } },
                    },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("Time") } } },
                },
            }));

        yield return Rule(new(
            "Composed unit with qualified unit reference",
            "kmh [kmh] EXTENDS Speed = ( OtherModel.km / OtherModel.h );",
            RefHB: "3.9.3-8",
            Expected: new UnitDef
            {
                Term = "kmh",
                Name = "kmh",
                NameLocations = { new RangePosition(0, 5, 0, 8) },
                Extends = new Reference<UnitDef> { Path = { new("Speed") } },
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Division,
                    FirstOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("OtherModel"), new("km") } } },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Path = { new("OtherModel"), new("h") } } },
                },
            }));

        yield return FullFile(new(
            "Define and use units",
            """
            INTERLIS 2.4;
            MODEL ModelName AT "foo:test" VERSION "123" =
                UNIT
                    Length (ABSTRACT);
                    Meter [m] EXTENDS Length;

                DOMAIN
                    Height = 0.00 .. 10.00 [m];
            END ModelName.
            """,
            Expected: TestTools.Build(() =>
            {
                var lengthUnit = new UnitDef { Name = "Length", Term = "Length", Properties = { Property.Abstract } };
                var meterUnit = new UnitDef { Name = "m", Term = "Meter", Extends = new Reference<UnitDef> { Target = lengthUnit, Path = { new("Length") } } };

                var heightDomain = new DomainDef
                {
                    Name = "Height",
                    TypeDef = new DecimalType
                    {
                        Min = 0,
                        Max = 10,
                        Precision = -2,
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                        Unit = new Reference<UnitDef> { Target = meterUnit, Path = { new("m") } },
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
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { new(InternalModel.Interlis.Name) } }) }
                                },
                                Content =
                                {
                                    { "Length", lengthUnit },
                                    { "m", meterUnit },
                                    { "Height", heightDomain },
                                }
                            }
                        }
                    }
                };
            })));

        yield return FullFile(new(
            "Unit extending itself",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                UNIT
                    u (ABSTRACT) EXTENDS u;
            END Model.
            """,
            Description: """
                A circular EXTENDS chain makes the unit transitively its own base (ili2c rejects the model too: its
                single-pass name resolution can not even resolve the self-reference).
                """,
            RefHB: "3.9.1-4",
            ExpectedLog: ["Type check error in 'Model.u' at 4:8-4:31: the unit transitively EXTENDS itself."],
            AssertOutput: false));

    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            UNIT
                {fragment}
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(UnitTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadUnitTypeDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitUnitTypeDef(p.unitTypeDef()));
    }
}
