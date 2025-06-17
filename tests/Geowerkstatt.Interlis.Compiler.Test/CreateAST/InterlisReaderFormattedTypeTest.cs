using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderFormattedTypeTest
{
    [TestMethod]
    public void ReadMinMax()
    {
        AssertReadRule("\"00\"..\"99\"",
            new FormattedType
            {
                Min = "00",
                Max = "99",
                SourceRange = new RangePosition(0, 0, 0, 10),
            });
    }

    [TestMethod]
    public void ReadMinMaxOfDomain()
    {
        AssertReadRule("""FORMAT INTERLIS.XMLDateTime "2000-01-01T00:00:00.000" .. "2000-12-31T23:59:59.999";""",
            new FormattedType
            {
                Min = "2000-01-01T00:00:00.000",
                Max = "2000-12-31T23:59:59.999",
                FormatBaseType = new Reference<DomainDef> { Path = { "INTERLIS", "XMLDateTime" }, ReferenceLocation = new RangePosition(0, 7, 0, 27) },
                SourceRange = new RangePosition(0, 0, 0, 82),
            });
    }

    [TestMethod]
    public void ReadFormatBasedOn()
    {
        AssertReadRule("""FORMAT BASED ON GregorianDate ( Year/4 "-" Month/2 "-" Day/2 );""",
            new FormattedType
            {
                BasedOn = new Reference<ClassDef> { Path = { "GregorianDate" }, ReferenceLocation = new RangePosition(0, 16, 0, 29) },
                SourceRange = new RangePosition(0, 0, 0, 62),
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitFormattedType(p.formattedType()));
}
