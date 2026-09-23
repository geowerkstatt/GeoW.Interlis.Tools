using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.TestTools;

namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// Verifies that every reference standing for a name written in the source is registered in its container's
/// <see cref="IInterlisDefinitionContainer.ContainerReferences"/>, whatever its <see cref="ReferenceResolution"/>.
/// That list is the only route a visitor has to a <see cref="Reference{T}"/> (see
/// <see cref="Interlis24AstBaseVisitor{TResult}"/>), so a consumer walking it — go-to-definition, find-all-
/// references, rename — sees an occurrence only if it is registered, and silently misses it otherwise.
/// <para>
/// Asserted here rather than through the AST comparison test cases because those ignore both
/// <see cref="Reference{T}.Resolution"/> and the container's reference list (see <see cref="TestTools"/>).
/// </para>
/// </summary>
public class ReferenceRegistrationTest
{
    /// <summary>
    /// A model exercising every member-resolved reference kind: an <c>OBJECTS OF</c> class, a
    /// <c>{basket.metaObject}</c> reference system and a <c>LOCAL UNIQUE</c> path.
    /// </summary>
    private const string MemberReferenceModel = """
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            REFSYSTEM BASKET Baskets ~ Systems OBJECTS OF Reference: Bern;

            TOPIC Systems =
                CLASS Reference EXTENDS INTERLIS.REFSYSTEM =
                END Reference;
            END Systems;

            TOPIC Topic =
                STRUCTURE Part =
                    Kind : TEXT*10;
                END Part;

                CLASS Measurement =
                    Height : 0.00 .. 9999.99 [INTERLIS.m] {Baskets.Bern};
                    Parts : LIST OF Part;
                    UNIQUE (LOCAL) Parts: Kind;
                END Measurement;
            END Topic;
        END Model.
        """;

    /// <summary>Every reference registered anywhere in <paramref name="environment"/>.</summary>
    private static IEnumerable<IReference> RegisteredReferences(InterlisEnvironment environment)
    {
        static IEnumerable<IReference> Walk(IInterlisDefinition definition)
        {
            if (definition is not IInterlisDefinitionContainer container)
            {
                yield break;
            }

            foreach (var reference in container.ContainerReferences)
            {
                yield return reference;
            }

            foreach (var nested in container.Content.Values.SelectMany(Walk))
            {
                yield return nested;
            }
        }

        return environment.Content.Values.SelectMany(Walk);
    }

    [Test]
    public async Task LocalUniquenessStepsAreRegisteredOnTheWritingClass()
    {
        var environment = await ReadWithoutErrors(MemberReferenceModel);
        var model = (ModelDef)environment.Content["Model"];
        var topic = (TopicDef)model.Content["Topic"];
        var measurement = (ClassDef)topic.Content["Measurement"];
        var local = measurement.Constraints.OfType<UniquenessConstraint>().Single().Local!;

        using (Assert.Multiple())
        {
            // The structure path is an object path, the attribute names are members of the structure it reaches.
            await Assert.That(Describe(local.StructurePath)).IsEqualTo("Parts@18:27-18:32 -> Model.Topic.Measurement -> Parts");
            await Assert.That(local.StructurePath.Resolution).IsEqualTo<ReferenceResolution>(ReferenceResolution.ObjectPath);
            await Assert.That(Describe(local.AttributeNames.Single())).IsEqualTo("Kind@18:34-18:38 -> Model.Topic.Part -> Kind");
            await Assert.That(local.AttributeNames.Single().Resolution).IsEqualTo<ReferenceResolution>(ReferenceResolution.Member);

            // Registered on the class that writes the constraint, not on the structure the names belong to.
            await Assert.That(measurement.ContainerReferences).Contains(local.StructurePath);
            await Assert.That(measurement.ContainerReferences).Contains(local.AttributeNames.Single());
        }
    }

    [Test]
    public async Task BasketMemberReferencesAreRegistered()
    {
        var environment = await ReadWithoutErrors(MemberReferenceModel);
        var model = (ModelDef)environment.Content["Model"];
        var basket = (MetaDataBasketDef)model.Content["Baskets"];
        var measurement = (ClassDef)((TopicDef)model.Content["Topic"]).Content["Measurement"];
        var height = (AttributeDef)measurement.Content["Height"];
        var refSystem = ((NumericType)height.TypeDef).RefSystem!;
        var metaObject = ((RefSys.MetaObjectRef)refSystem.Value!).MetaObject!;

        using (Assert.Multiple())
        {
            // OBJECTS OF names a class of the basket's topic — a member name, registered on the enclosing model.
            await Assert.That(Describe(basket.Objects.Single().Class)).IsEqualTo("Reference@3:50-3:59 -> Model.Systems.Reference");
            await Assert.That(model.ContainerReferences).Contains(basket.Objects.Single().Class);

            // {Baskets.Bern}: the basket prefix is scoped, the meta-object name is a member of that basket.
            await Assert.That(Describe(metaObject)).IsEqualTo("Bern@16:59-16:63 -> Bern");
            await Assert.That(metaObject.Resolution).IsEqualTo<ReferenceResolution>(ReferenceResolution.Member);
            await Assert.That(measurement.ContainerReferences).Contains(metaObject);

            var basketPrefix = ((RefSys.MetaObjectRef)refSystem.Value!).Basket!;
            await Assert.That(Describe(basketPrefix)).IsEqualTo("Baskets@16:51-16:58 -> Model.Baskets");
            await Assert.That(basketPrefix.Resolution).IsEqualTo<ReferenceResolution>(ReferenceResolution.Scoped);
        }
    }

    [Test]
    public async Task EveryRegisteredReferenceOfAValidModelResolves()
    {
        var environment = await ReadWithoutErrors(MemberReferenceModel);

        var unresolved = RegisteredReferences(environment).Where(r => r.Target == null).Select(Describe);

        await Assert.That(unresolved).IsEquivalentTo(Array.Empty<string>());
    }

    [Test]
    public async Task AMemberNameIsNotBoundToASameNamedDefinitionInScope()
    {
        // 'Kind' names both a DOMAIN in the model scope and an attribute of the structure. The LOCAL UNIQUE step
        // must reach the attribute: looking the name up in the lexical scopes would find the domain, reject it as
        // no AttributeDef and report the name as unresolvable — which ReadWithoutErrors would fail on.
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Kind = (a, b);

                TOPIC Topic =
                    STRUCTURE Part =
                        Kind : TEXT*10;
                    END Part;

                    CLASS Measurement =
                        Parts : LIST OF Part;
                        UNIQUE (LOCAL) Parts: Kind;
                    END Measurement;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var measurement = (ClassDef)topic.Content["Measurement"];
        var local = measurement.Constraints.OfType<UniquenessConstraint>().Single().Local!;

        await Assert.That(Describe(local.AttributeNames.Single())).IsEqualTo("Kind@13:34-13:38 -> Model.Topic.Part -> Kind");
    }
}
