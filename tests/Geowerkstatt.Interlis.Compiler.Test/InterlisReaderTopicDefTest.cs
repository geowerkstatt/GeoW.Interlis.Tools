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
                new TopicDef { FullyQualifiedName = new Identifier { Topic = "Test" } });
    }

    [TestMethod]
    public void ReadTopicDefComplete()
    {
        AssertReadRule("""
                /** Doc-Comment */
                !!@ key=value
                VIEW TOPIC Test_A (ABSTRACT, EXTENDED) EXTENDS Test_B =
                    BASKET OID AS oidDomain;
                    OID AS INTERLIS.UUIDOID;
                    DEPENDS ON Test_C, Test_D;
                    DEFERRED GENERICS genericA, genericB;
                END Test_A;
                """,
                new TopicDef
                {
                    FullyQualifiedName = new Identifier { Topic = "Test_A" },
                    DocComments = { "/** Doc-Comment */" },
                    MetaAttributes = { { "key", "value" } }
                });
    }

    private void AssertReadRule(string input, object? expected)
    {
        var actual = new InterlisReader().ReadRule(new StringReader(input), (p, v) => v.VisitTopicDef(p.topicDef()));
        expected.ShouldDeepEqual(actual);
    }
}
