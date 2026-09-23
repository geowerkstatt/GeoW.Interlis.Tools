using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using static Geowerkstatt.Interlis.Compiler.TestTools;

namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// Verifies that each dot-separated segment of a reference path carries its own span and its own target. A rename
/// replaces one name: renaming a class reached as <c>Model.Topic.ClassA</c> must rewrite only the last segment,
/// and renaming the topic only the middle one — so every segment has to know both where it is written and what it
/// denotes; the reference's overall <see cref="Reference{T}.SourceRange"/> and <see cref="Reference{T}.Target"/>
/// are the wrong unit for either.
/// <para>
/// Asserted here rather than through the AST comparison test cases because those ignore a segment's span and
/// target (see <see cref="TestTools"/>): the expected paths there are written as plain names.
/// </para>
/// </summary>
public class ReferencePathSegmentTest
{
    private const string QualifiedExtendsModel = """
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                CLASS Base =
                END Base;
            END Topic;

            TOPIC Other =
                CLASS Derived EXTENDS Model.Topic.Base =
                END Derived;
            END Other;
        END Model.
        """;

    /// <summary>Renders what each segment denotes as <c>name -&gt; target</c>, one per line.</summary>
    private static string DescribeTargets(IReference reference)
        => string.Join("\n", reference.Path.Select(segment => $"{segment.Name} -> {(segment.Target is IInterlisDefinition definition ? definition.FullyQualifiedName : segment.Target?.Name ?? "<unresolved>")}"));

    private static Reference<ClassDef> DerivedExtends(InterlisEnvironment environment)
    {
        var model = (ModelDef)environment.Content["Model"];
        return ((ClassDef)((TopicDef)model.Content["Other"]).Content["Derived"]).Extends!;
    }

    [Test]
    public async Task EachSegmentOfAQualifiedReferenceHasItsOwnSpan()
    {
        var environment = await ReadWithoutErrors(QualifiedExtendsModel);
        var extends = DerivedExtends(environment);

        using (Assert.Multiple())
        {
            await Assert.That(Describe(extends)).IsEqualTo("Model@9:30-9:35.Topic@9:36-9:41.Base@9:42-9:46 -> Model.Topic.Base");

            // The reference as a whole still spans the full qualification.
            await Assert.That(extends.SourceRange!.Start.Character).IsEqualTo(30);
            await Assert.That(extends.SourceRange!.End.Character).IsEqualTo(46);

            // The segment a rename of the target rewrites is the last one; it names exactly the target.
            await Assert.That(extends.Path[^1].Name).IsEqualTo(extends.Target!.Name);
        }
    }

    [Test]
    public async Task EachSegmentOfAQualifiedReferenceDenotesTheDefinitionItNames()
    {
        var environment = await ReadWithoutErrors(QualifiedExtendsModel);

        // Renaming the topic has to rewrite the middle segment, so that segment must know it denotes the topic.
        await Assert.That(DescribeTargets(DerivedExtends(environment))).IsEqualTo("""
            Model -> Model
            Topic -> Model.Topic
            Base -> Model.Topic.Base
            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task AQualificationDenotesTheContainerWrittenNotTheDeclaringOne()
    {
        // 'A' is declared in Base and reached through the extending topic Ext (RefHB 3.5.4-11). The segment 'Ext'
        // denotes Ext — what a rename of Ext must rewrite — even though the target's parent is Base. Walking the
        // target's parent chain would attribute the segment to Base and leave the written name stale.
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Base =
                    CLASS A =
                    END A;
                END Base;

                TOPIC Ext EXTENDS Base =
                    CLASS B EXTENDS Model.Ext.A =
                    END B;
                END Ext;
            END Model.
            """);

        var model = (ModelDef)environment.Content["Model"];
        var extends = ((ClassDef)((TopicDef)model.Content["Ext"]).Content["B"]).Extends!;

        await Assert.That(DescribeTargets(extends)).IsEqualTo("""
            Model -> Model
            Ext -> Model.Ext
            A -> Model.Base.A
            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task AnImportDenotesTheImportedModel()
    {
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Other AT "http://example.com" VERSION "1.0.0" =
            END Other.

            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                IMPORTS Other;
            END Model.
            """);

        var model = (ModelDef)environment.Content["Model"];

        // Renaming a model has to rewrite the IMPORTS that name it.
        await Assert.That(Describe(model.Imports["Other"].ModelDef)).IsEqualTo("Other@6:12-6:17 -> Other");
        await Assert.That(DescribeTargets(model.Imports["Other"].ModelDef)).IsEqualTo("Other -> Other");
    }

    [Test]
    public async Task AnAttributePathConstantSpansViewableAndAttributeSeparately()
    {
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        Flag : BOOLEAN;
                        MANDATORY CONSTRAINT >>Model.Topic.ClassA->Flag == #true;
                    END ClassA;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var classA = (ClassDef)topic.Content["ClassA"];
        var constant = (AttributePathConstant)((ComparisonExpression)classA.Constraints.OfType<MandatoryConstraint>().Single().Condition!).FirstOperand;

        using (Assert.Multiple())
        {
            // '>>Model.Topic.ClassA->Flag' is one reference to the attribute whose last segment is the attribute name.
            await Assert.That(Describe(constant.Attribute)).IsEqualTo("Model@6:35-6:40.Topic@6:41-6:46.ClassA@6:47-6:53.Flag@6:55-6:59 -> Model.Topic.ClassA -> Flag");
            await Assert.That(DescribeTargets(constant.Attribute)).IsEqualTo("""
                Model -> Model
                Topic -> Model.Topic
                ClassA -> Model.Topic.ClassA
                Flag -> Model.Topic.ClassA -> Flag
                """.ReplaceLineEndings("\n"));
        }
    }

    [Test]
    public async Task AnImpliedPathHasNoSpansButResolves()
    {
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        NO OID;
                    END ClassA;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var classA = (ClassDef)topic.Content["ClassA"];

        using (Assert.Multiple())
        {
            // NO OID stands for INTERLIS.NOOID, which the source never writes: nothing a rename could rewrite, ...
            await Assert.That(Describe(classA.OidType!)).IsEqualTo("INTERLIS@<none>.NOOID@<none> -> INTERLIS.NOOID");

            // ... but the segments still know what they denote, like any other resolved path.
            await Assert.That(DescribeTargets(classA.OidType!)).IsEqualTo("INTERLIS -> INTERLIS\nNOOID -> INTERLIS.NOOID");
        }
    }

    [Test]
    public async Task AnImplicitBaseNameIsDeclaredByTheViewableReference()
    {
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        Flag : BOOLEAN;
                    END ClassA;

                    VIEW Implicit
                        PROJECTION OF ClassA;
                        WHERE DEFINED(ClassA->Flag);
                        =
                        ALL OF ClassA;
                    END Implicit;

                    VIEW Renamed
                        PROJECTION OF a~ClassA;
                        =
                        ALL OF a;
                    END Renamed;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var implicitView = (ViewDef)topic.Content["Implicit"];
        var implicitBase = (BaseView)implicitView.Content["ClassA"];
        var renamedBase = (BaseView)((ViewDef)topic.Content["Renamed"]).Content["a"];

        using (Assert.Multiple())
        {
            // 'PROJECTION OF ClassA' declares the base name ClassA with the very token that references the class: the
            // base name has no other place in the source a rename could write to or navigation could land on.
            await Assert.That(string.Join(",", implicitBase.NameLocations)).IsEqualTo("9:26-9:32");
            await Assert.That(implicitBase.NameLocations.Single().ToString()).IsEqualTo(implicitBase.Viewable!.Path.Single().Range!.ToString());

            // The uses of the base name denote the base view, not the class it stands for.
            await Assert.That(Describe(implicitView.AllOfBases.Single())).IsEqualTo("ClassA@12:19-12:25 -> Model.Topic.Implicit.ClassA");
            var selection = (PathExpression)((DefinedExpression)implicitView.Selections.Single()).Operand;
            await Assert.That(Describe(selection.Reference.Path[0])).IsEqualTo("ClassA@10:26-10:32 -> Model.Topic.Implicit.ClassA");

            // An explicit alias is declared by the alias token, as before.
            await Assert.That(string.Join(",", renamedBase.NameLocations)).IsEqualTo("16:26-16:27");
        }
    }
}
