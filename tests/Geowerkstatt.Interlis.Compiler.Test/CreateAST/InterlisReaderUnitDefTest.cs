using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderUnitDefTest
{
    [TestMethod]
    public void ReadAbstractUnit()
    {
        AssertReadRule("Length (ABSTRACT);",
            new UnitDef
            {
                Name = "Length",
                Term = "Length",
                NameLocations = { new RangePosition(0, 0, 0, 6) },
                Properties = { Property.Abstract },
            });
    }

    [TestMethod]
    public void ReadExtendingUnit()
    {
        AssertReadRule("Meter [m] EXTENDS Length;",
            new UnitDef
            {
                Name = "m",
                Term = "Meter",
                NameLocations = { new RangePosition(0, 7, 0, 8) },
                Extends = new Reference<UnitDef> { Path = { "Length" }, ReferenceLocation = new RangePosition(0, 18, 0, 24) },
            });
    }

    [TestMethod]
    public void ReadAbstractComposedUnit()
    {
        AssertReadRule("Area (ABSTRACT) = (Length * Length);",
            new UnitDef
            {
                Name = "Area",
                Term = "Area",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                Properties = { Property.Abstract },
                Expression = new Multiplication
                {
                    FirstOperand = new PathExpression
                    {
                        Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "Length" }, ReferenceLocation = new RangePosition(0, 19, 0, 25) } } }
                    },
                    SecondOperand = new PathExpression
                    {
                        Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "Length" }, ReferenceLocation = new RangePosition(0, 28, 0, 34) } } },
                    }
                }
            });
    }

    [TestMethod]
    public void ReadComposedUnit()
    {
        AssertReadRule("KilometersPerHour [kmh] EXTENDS Speed = (km / h);",
            new UnitDef
            {
                Name = "kmh",
                Term = "KilometersPerHour",
                NameLocations = { new RangePosition(0, 19, 0, 22) },
                Extends = new Reference<UnitDef> { Path = { "Speed" }, ReferenceLocation = new RangePosition(0, 32, 0, 37) },
                Expression = new Division
                {
                    FirstOperand = new PathExpression
                    {
                        Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "km" }, ReferenceLocation = new RangePosition(0, 41, 0, 43) } } }
                    },
                    SecondOperand = new PathExpression
                    {
                        Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "h" }, ReferenceLocation = new RangePosition(0, 46, 0, 47) } } },
                    }
                }
            });
    }

    [TestMethod]
    public void ReadDerivedUnit()
    {
        AssertReadRule("AngleDegree = 360 / 2 / PI [AngleRad];",
            new UnitDef
            {
                Name = "AngleDegree",
                Term = "AngleDegree",
                NameLocations = { new RangePosition(0, 0, 0, 11) },
                Expression = new Multiplication
                {
                    FirstOperand = new NumericConstant
                    {
                        Value = 57.29577951308232,
                    },
                    SecondOperand = new PathExpression
                    {
                        Path =
                        {
                            new ReferencePathElement
                            {
                                Value = new Reference<IInterlisDefinition>
                                {
                                    Path = { "AngleRad" },
                                    ReferenceLocation = new RangePosition(0, 28, 0, 36)
                                }
                            }
                        },
                    }
                }
            });
    }

    [TestMethod]
    public void ReadUnitWithDoc()
    {
        AssertReadRule("""
            /** Base unit for all temperatures. */
            !!@ meta=value
            Temperature (ABSTRACT);
            """,
            new UnitDef
            {
                Name = "Temperature",
                Term = "Temperature",
                NameLocations = { new RangePosition(2, 0, 2, 11) },
                Properties = { Property.Abstract },
                DocComments = { "/** Base unit for all temperatures. */" },
                MetaAttributes = { { "meta", "value" } },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitUnitTypeDef(p.unitTypeDef()));
}
