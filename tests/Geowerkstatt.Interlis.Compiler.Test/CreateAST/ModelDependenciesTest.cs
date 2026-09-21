using Geowerkstatt.Interlis.Compiler.AST;

namespace Geowerkstatt.Interlis.Compiler;

public class ModelDependenciesTest
{
    [Test]
    public async Task DependenciesAreTheImportsAndTheTranslationBase()
    {
        var environment = new InterlisReader().ReadRule(
            new StringReader(
                """
                INTERLIS 2.4;
                MODEL English (en) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Deutsch [ "1.0.0" ] =
                  IMPORTS Units, GeometryCHLV95_V1;
                END English.
                """),
            (parser, visitor) => visitor.VisitInterlis(parser.interlis()));

        var model = (ModelDef)environment.Content["English"];
        var dependencies = model.Dependencies.Select(d => d.ModelName).ToList();

        await Assert.That(dependencies).IsEquivalentTo(["INTERLIS", "Units", "GeometryCHLV95_V1", "Deutsch"]);
    }
}
