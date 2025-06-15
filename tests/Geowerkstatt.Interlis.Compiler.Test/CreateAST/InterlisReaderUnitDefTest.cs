using Geowerkstatt.Interlis.Compiler.AST;

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
                Extends = new Reference<UnitDef> { Path = { "Length" } },
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
            });
    }

    [TestMethod]
    public void ReadComposedUnit()
    {
        AssertReadRule("SquareMeter [m2] EXTENDS Area = (m * m);",
            new UnitDef
            {
                Name = "m2",
                Term = "SquareMeter",
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 13 }, End = new Position { Line = 0, Character = 15 } } },
                Extends = new Reference<UnitDef> { Path = { "Area" } },
            });
    }

    [TestMethod]
    public void ReadDerivedUnit()
    {
        AssertReadRule("AngleDegree = 180 / PI [AngleRad];",
            new UnitDef
            {
                Name = "AngleDegree",
                Term = "AngleDegree",
                NameLocations = { new RangePosition { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 11 } } },
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
