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
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 6 } } },
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
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 7 }, End = new Position { Line = 0, Character = 8 } } },
                Extends = new Reference<UnitDef> { Path = { "Length" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 0, Character = 18 }, End = new Position { Line = 0, Character = 24 } } },
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
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 4 } } },
                Properties = { Property.Abstract },
                Expression = new Multiplication
                {
                    FirstOperand = new PathExpression
                    {
                        Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "Length" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 0, Character = 19 }, End = new Position { Line = 0, Character = 25 } } } } }
                    },
                    SecondOperand = new PathExpression
                    {
                        Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "Length" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 0, Character = 28 }, End = new Position { Line = 0, Character = 34 } } } } },
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
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 19 }, End = new Position { Line = 0, Character = 22 } } },
                Extends = new Reference<UnitDef> { Path = { "Speed" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 0, Character = 32 }, End = new Position { Line = 0, Character = 37 } } },
                Expression = new Division
                {
                    FirstOperand = new PathExpression
                    {
                        Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "km" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 0, Character = 41 }, End = new Position { Line = 0, Character = 43 } } } } }
                    },
                    SecondOperand = new PathExpression
                    {
                        Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Path = { "h" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 0, Character = 46 }, End = new Position { Line = 0, Character = 47 } } } } },
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
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 11 } } },
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
                                    ReferenceLocation = new RangePosition { Start = new Position { Line = 0, Character = 28 }, End = new Position { Line = 0, Character = 36 } }
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
                NameLocations = { new RangePosition { Start = new Position { Line = 2, Character = 0 }, End = new Position { Line = 2, Character = 11 } } },
                Properties = { Property.Abstract },
                DocComments = { "/** Base unit for all temperatures. */" },
                MetaAttributes = { { "meta", "value" } },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitUnitTypeDef(p.unitTypeDef()));
}
