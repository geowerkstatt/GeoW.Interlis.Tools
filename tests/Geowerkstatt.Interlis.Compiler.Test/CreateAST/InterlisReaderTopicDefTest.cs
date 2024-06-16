using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools;
using Geowerkstatt.Interlis.Tools.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools;

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
                new TopicDef { Name = "Test" });
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
                    DocComments = { "/** Doc-Comment */" },
                    MetaAttributes = { { "key", "value" } }
                });
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitTopicDef(p.topicDef()));
}
