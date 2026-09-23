using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.TestTools;

namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// Verifies that a <c>{[basket.]metaObject}</c> reference system links to the <see cref="MetaObjectDeclaration"/> of
/// a basket (RefHB 3.10.1): the qualified form to the named basket's, the unqualified form to the first visible
/// basket's — the enclosing containers' baskets innermost first, then those of the imports allowing unqualified
/// names. A meta object is data, so the link is a navigation anchor, not a validation: which basket supplies the
/// object at runtime may differ (RefHB 3.10.1-3), and an unqualified name no visible basket declares is left
/// unresolved without a report.
/// </summary>
public class MetaObjectReferenceTest
{
    private const string BasketModel = """
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            REFSYSTEM BASKET Baskets ~ Systems OBJECTS OF Reference: Bern, Zurich;

            TOPIC Systems =
                CLASS Reference EXTENDS INTERLIS.REFSYSTEM =
                END Reference;
            END Systems;

            TOPIC Topic =
                REFSYSTEM BASKET Local ~ Systems OBJECTS OF Reference: Zurich;

                CLASS Measurement =
                    Qualified : 0.00 .. 9999.99 [INTERLIS.m] {Baskets.Bern};
                    FromModel : 0.00 .. 9999.99 [INTERLIS.m] {Bern};
                    FromTopic : 0.00 .. 9999.99 [INTERLIS.m] {Zurich};
                    Unknown : 0.00 .. 9999.99 [INTERLIS.m] {Nowhere};
                END Measurement;
            END Topic;
        END Model.
        """;

    private static MetaObjectDeclaration Declared(ModelDef model, string basketPath, string name)
    {
        IInterlisDefinitionContainer container = model;
        var segments = basketPath.Split('.');
        foreach (var segment in segments[..^1])
        {
            container = (IInterlisDefinitionContainer)container.Content[segment];
        }

        return ((MetaDataBasketDef)container.Content[segments[^1]]).Objects.SelectMany(objects => objects.MetaObjects).Single(declaration => declaration.Name == name);
    }

    private static Reference<MetaObjectDeclaration> RefSystemOf(ModelDef model, string attribute)
    {
        var measurement = (ClassDef)((TopicDef)model.Content["Topic"]).Content["Measurement"];
        var type = (NumericType)((AttributeDef)measurement.Content[attribute]).TypeDef!;
        return ((RefSys.MetaObjectRef)type.RefSystem!.Value!).MetaObject!;
    }

    [Test]
    public async Task AQualifiedNameLinksToTheNamedBasketsDeclaration()
    {
        var environment = await ReadWithoutErrors(BasketModel);
        var model = (ModelDef)environment.Content["Model"];

        var reference = RefSystemOf(model, "Qualified");

        using (Assert.Multiple())
        {
            await Assert.That(reference.Target).IsSameReferenceAs(Declared(model, "Baskets", "Bern"));
            await Assert.That(Describe(reference.Path[0])).IsEqualTo("Bern@14:62-14:66 -> Bern");
        }
    }

    [Test]
    public async Task AnUnqualifiedNameLinksToTheFirstVisibleBasketDeclaringIt()
    {
        var environment = await ReadWithoutErrors(BasketModel);
        var model = (ModelDef)environment.Content["Model"];

        using (Assert.Multiple())
        {
            // Bern is declared by the model-level basket only; Zurich by both, and the topic's own basket is nearer.
            await Assert.That(RefSystemOf(model, "FromModel").Target).IsSameReferenceAs(Declared(model, "Baskets", "Bern"));
            await Assert.That(RefSystemOf(model, "FromTopic").Target).IsSameReferenceAs(Declared(model, "Topic.Local", "Zurich"));
        }
    }

    [Test]
    public async Task AnUnqualifiedNameNoBasketDeclaresStaysUnresolvedWithoutAReport()
    {
        // ReadWithoutErrors asserts that nothing was reported.
        var environment = await ReadWithoutErrors(BasketModel);
        var model = (ModelDef)environment.Content["Model"];

        await Assert.That(RefSystemOf(model, "Unknown").Target).IsNull();
    }
}
