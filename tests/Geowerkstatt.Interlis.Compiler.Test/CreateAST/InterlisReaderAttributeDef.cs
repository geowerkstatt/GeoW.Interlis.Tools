using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderAttributeDef
{
    [TestMethod]
    public void ReadTextAttributeDef()
    {
        AssertReadRule("Attr : TEXT*12;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 14), },
            });
    }

    [TestMethod]
    public void ReadTextAttributeDefWithoutLength()
    {
        AssertReadRule("Attr : TEXT;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType { Length = null, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 11), },
            });
    }

    [TestMethod]
    public void ReadNumericAttributeDef()
    {
        AssertReadRule("Attr : 000..999;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new NumericType { Min = 0, Max = 999, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 15), },
            });
    }

    [TestMethod]
    public void ReadBooleanAttributeDef()
    {
        AssertReadRule("Attr : BOOLEAN;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 14) },
            });
    }

    [TestMethod]
    public void ReadQualifiedBooleanAttributeDef()
    {
        AssertReadRule("Attr : INTERLIS.BOOLEAN;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 7, 0, 23), },
            });
    }

    [TestMethod]
    public void ReadCoordAttributeDef()
    {
        AssertReadRule("Attr : COORD 0..100, 0..100;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new CoordType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Axis =
                    {
                        new NumericType { Min = 0, Max = 100, Precision = 0, SourceRange = new RangePosition(0, 13, 0, 19), },
                        new NumericType { Min = 0, Max = 100, Precision = 0, SourceRange = new RangePosition(0, 21, 0, 27), },
                    },
                    SourceRange = new RangePosition(0, 7, 0, 27),
                },
            });
    }

    [TestMethod]
    public void ReadEnumerationAttributeDef()
    {
        AssertReadRule("Attr : MANDATORY (red (lightRed, darkRed), green, blue (lightBlue, darkBlue : FINAL)) ORDERED;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationType
                {
                    Cardinality = new Cardinality { Min = 1, Max = 1 },
                    Sequencing = EnumerationType.Sequencings.Ordered,
                    SourceRange = new RangePosition(0, 17, 0, 93),
                    Values =
                    {
                        new EnumerationTreeNode
                        {
                            Name = "red",
                            SubValues =
                            {
                                new EnumerationTreeNode { Name = "lightRed" },
                                new EnumerationTreeNode { Name = "darkRed" },
                            }
                        },
                        new EnumerationTreeNode { Name = "green" },
                        new EnumerationTreeNode
                        {
                            Name = "blue",
                            SubValues =
                            {
                                new EnumerationValuesList(isFinal : true)
                                {
                                    new EnumerationTreeNode { Name = "lightBlue" },
                                    new EnumerationTreeNode { Name = "darkBlue" },
                                }
                            }
                        },
                    },
                },
            });
    }

    [TestMethod]
    public void ReadBlackboxXmlAttributeDef()
    {
        AssertReadRule("Attr : BLACKBOX XML;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BlackboxType
                {
                    Kind = BlackboxType.BlackboxTypeKind.Xml,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 19),
                },
            });
    }

    [TestMethod]
    public void ReadBlackboxBinaryAttributeDef()
    {
        AssertReadRule("Attr : BLACKBOX BINARY;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new BlackboxType
                {
                    Kind = BlackboxType.BlackboxTypeKind.Binary,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 22),
                },
            });
    }

    [TestMethod]
    public void ReadDateAttributeDef()
    {
        AssertReadRule("Attr : DATE;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Extends = new Reference<DomainDef> { Path = { "INTERLIS", "XMLDate" } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                },
            });
    }

    [TestMethod]
    public void ReadTimeAttributeDef()
    {
        AssertReadRule("Attr : TIMEOFDAY;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Extends = new Reference<DomainDef> { Path = { "INTERLIS", "XMLTime" } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                },
            });
    }

    [TestMethod]
    public void ReadDateTimeAttributeDef()
    {
        AssertReadRule("Attr : DATETIME;",
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Extends = new Reference<DomainDef> { Path = { "INTERLIS", "XMLDateTime" } },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                },
            });
    }

    [TestMethod]
    public void ReadAttributeDefComplete()
    {
        AssertReadRule("""
            !!@ key=value
            /** Doc-Comment */
            CONTINUOUS SUBDIVISION Attr (ABSTRACT, EXTENDED) : 0.00 .. 100.00 := PI, 0.1230e-10;
            """,
            new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(2, 23, 2, 27) },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                TypeDef = new NumericType { Min = 0, Max = 100, Precision = -2, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 51, 2, 65), },
                Properties = { Property.Abstract, Property.Extended },
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitAttributeDef(p.attributeDef()));
}
