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
                    new RangePosition(0, 6, 0, 10),
                    new RangePosition(1, 4, 1, 8)
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
                    new RangePosition(2, 11, 2, 17),
                    new RangePosition(7, 4, 7, 10)
                },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                Extends = new Reference<TopicDef> { Path = { "Test_B" }, ReferenceLocation = new RangePosition(2, 44, 2, 50) },
                BasketOidType = new Reference<DomainDef> { Path = { "oidDomain" }, ReferenceLocation = new RangePosition(3, 18, 3, 27) },
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "UUIDOID" }, ReferenceLocation = new RangePosition(4, 11, 4, 27) },
                Properties = { Property.Abstract, Property.Final }
            });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitTopicDef(p.topicDef()));
}
