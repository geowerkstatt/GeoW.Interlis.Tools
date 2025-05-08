using Geowerkstatt.Interlis.Tools.AST;

namespace Geowerkstatt.Interlis.Tools;

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
                Properties = { Property.Abstract },
                DocComments = { "/** Base unit for all temperatures. */" },
                MetaAttributes = { { "meta", "value" } },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitUnitTypeDef(p.unitTypeDef()));
}
