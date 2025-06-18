using Antlr4.Runtime.Misc;
using DeepEqual.Syntax;
using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.CreateAST;

namespace Geowerkstatt.Interlis.Compiler;

[TestClass]
public class InterlisReaderModelDefTest
{
    [TestMethod]
    public void ReadModelDef()
    {
        AssertReadRule("""
            MODEL Test AT "foo.test" VERSION "123" =
            END Test.
            """,
            new ModelDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 10),
                    new RangePosition(1, 4, 1, 8)
                },
                URI = "foo.test",
                Version = "123",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            });
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
                NameLocations =
                {
                    new RangePosition(2, 22, 2, 28),
                    new RangePosition(7, 4, 7, 10)
                },
                DocComments = { "/** A model with all optional fields set */" },
                MetaAttributes = { { "EPSG", "2056" } },
                Language = "en",
                URI = "foo.test",
                Version = "123",
                Xmlns = "http://www.interlis.test",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name }, ReferenceLocation = null }) }, // Implicit import
                    { "Test_C", (true, new Reference<ModelDef> { Path = { "Test_C" }, ReferenceLocation = new RangePosition(6, 24, 6, 30) }) }
                },
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
                NameLocations =
                {
                    new RangePosition(3, 6, 3, 10),
                    new RangePosition(4, 4, 4, 8)
                },
                DocComments = { string.Join(Environment.NewLine, "/**", " * Documentation String", " */") },
                URI = "foo.test",
                Version = "123",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
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
                NameLocations =
                {
                    new RangePosition(1, 6, 1, 10),
                    new RangePosition(2, 4, 2, 8)
                },
                MetaAttributes = { { "key1", "value with spaces and escapes: \" \\ ø \U0001F60E" }, { "key2", "#ff1234/256.0e-10" } },
                URI = "foo.test",
                Version = "123",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            });
    }

    [TestMethod]
    public void ReadModelDefWithDuplicateMetaAttributes()
    {
        var logs = GetLogMessages("""
            !!@ KEY_A = red; KEY_B = 1; KEY_B = 2
            !!@ OTHER_KEY = "value"; KEY_A = green
            MODEL Test AT "foo.test" VERSION "123" =
            END Test.
            """);

        Assert.AreEqual("Compile error at line 3:0 modelDef has meta attributes with duplicate keys: 'KEY_A', 'KEY_B'.", logs.FirstOrDefault());
    }

    private void AssertReadRule(string input, object? expected)
        => InterlisReaderInterlisFileTest.AssertReadRule(input, expected, (p, v) => v.VisitModelDef(p.modelDef()));

    private List<string> GetLogMessages(string input)
        => InterlisReaderInterlisFileTest.GetLogMessages(input, (p, v) => v.VisitModelDef(p.modelDef()));
}
