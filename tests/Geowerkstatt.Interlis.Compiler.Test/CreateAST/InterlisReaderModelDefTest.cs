using Antlr4.Runtime.Misc;
using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Tools.AST;

namespace Geowerkstatt.Interlis.Tools;

[TestClass]
public class InterlisReaderModelDefTest
{
    [TestMethod]
    public void ReadModelDef()
    {
        AssertReadRule("""
            MODEL Test AT "foo.test" VERSION "123" =
            END Test.
            """, new ModelDef { Name = "Test", URI = "foo.test", Version = "123" });
    }

    [TestMethod]
    public void ReadModelDefComplete()
    {
        AssertReadRule("""
            /** A model with all optional fields set */
            !!@ EPSG=2056
            CONTRACTED TYPE MODEL Test_A (en) NOINCREMENTALTRANSFER AT "foo.test" VERSION "123" // Version Explanation //
            TRANSLATION OF Test_B ["12"] =
                CHARSET "UTF-32";
                XMLNS "http://www.interlis.test";
                IMPORTS UNQUALIFIED Test_C;
            END Test_A.
            """,
            new ModelDef
            {
                Name = "Test_A",
                DocComments = { "/** A model with all optional fields set */" },
                MetaAttributes = { { "EPSG", "2056" } },
                Language = "en",
                URI = "foo.test",
                Version = "123",
                Xmlns = "http://www.interlis.test",
            });
    }

    [TestMethod]
    public void ReadModelDefWithDocComment()
    {
        AssertReadRule("""
            /**
             * Documentation String
             */
            MODEL Test AT "foo.test" VERSION "123" =
            END Test.
            """,
            new ModelDef
            {
                Name = "Test",
                DocComments = { string.Join(Environment.NewLine, "/**", " * Documentation String", " */") },
                URI = "foo.test",
                Version = "123",
            });
    }

    [TestMethod]
    public void ReadModelDefWithMetaAttributes()
    {
        AssertReadRule("""
            !!@ key1 = "value with spaces and escapes: \" \\ \u00f8 \uD83D\uDE0E"; key2 = #ff1234/256.0e-10
            MODEL Test AT "foo.test" VERSION "123" =
            END Test.
            """,
            new ModelDef
            {
                Name = "Test",
                MetaAttributes = { { "key1", "value with spaces and escapes: \" \\ ø \U0001F60E" }, { "key2", "#ff1234/256.0e-10" } },
                URI = "foo.test",
                Version = "123",
            });
    }

    [TestMethod]
    public void ReadModelDefWithDuplicateMetaAttributes()
    {
        var ex = Assert.ThrowsException<ParseCanceledException>(() =>
        {
            AssertReadRule("""
                !!@ KEY_A = red; KEY_B = 1; KEY_B = 2
                !!@ OTHER_KEY = "value"; KEY_A = green
                MODEL Test AT "foo.test" VERSION "123" =
                END Test.
                """,
                null);
        });

        Assert.AreEqual("Compile error at line 1:0 modelDef has meta attributes with duplicate keys: 'KEY_A', 'KEY_B'.", ex.Message);
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitModelDef(p.modelDef()));
}
