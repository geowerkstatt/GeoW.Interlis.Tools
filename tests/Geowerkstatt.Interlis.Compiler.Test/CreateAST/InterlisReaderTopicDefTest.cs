using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderTopicDefTest
{
    [TestMethod]
    public void ReadTopicDef()
    {
        AssertReadRule("""
            TOPIC Test =
            END Test;
            """,
            new TopicDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition { Start = new Position { Line = 0, Character = 6 }, End = new Position { Line = 0, Character = 10 } },
                    new RangePosition { Start = new Position { Line = 1, Character = 4 }, End = new Position { Line = 1, Character = 8 } }
                },
            });
    }

    [TestMethod]
    public void ReadTopicDefComplete()
    {
        AssertReadRule("""
            /** Doc-Comment */
            !!@ key=value
            VIEW TOPIC Test_A (ABSTRACT, FINAL) EXTENDS Test_B =
                BASKET OID AS oidDomain;
                OID AS INTERLIS.UUIDOID;
                DEPENDS ON Test_C, Test_D;
                DEFERRED GENERICS genericA, genericB;
            END Test_A;
            """,
            new TopicDef
            {
                Name = "Test_A",
                NameLocations =
                {
                    new RangePosition { Start = new Position { Line = 2, Character = 11 }, End = new Position { Line = 2, Character = 17 } },
                    new RangePosition { Start = new Position { Line = 7, Character = 4 }, End = new Position { Line = 7, Character = 10 } }
                },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                Extends = new Reference<TopicDef> { Path = { "Test_B" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 2, Character = 44 }, End = new Position { Line = 2, Character = 50 } } },
                BasketOidType = new Reference<DomainDef> { Path = { "oidDomain" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 3, Character = 18 }, End = new Position { Line = 3, Character = 27 } } },
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "UUIDOID" }, ReferenceLocation = new RangePosition { Start = new Position { Line = 4, Character = 11 }, End = new Position { Line = 4, Character = 27 } } },
                Properties = { Property.Abstract, Property.Final }
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitTopicDef(p.topicDef()));
}
