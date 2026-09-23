using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using static Geowerkstatt.Interlis.Compiler.TestTools;

namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// Verifies that an object or attribute path (RefHB 3.13) is one <see cref="ReferenceResolution.ObjectPath"/>
/// reference whose segments are its steps, each carrying the span of the name it writes and the definition the path
/// resolver reached there. A rename of an attribute has to update every step that names it, not only the paths that
/// end on it, so the tip alone is not enough.
/// <para>
/// Asserted here rather than through the AST comparison test cases because those ignore a segment's span and
/// target and a reference's target when the expectation states none (see <see cref="TestTools"/>).
/// </para>
/// </summary>
public class ObjectPathReferenceTest
{
    private const string PathModel = """
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                STRUCTURE Part =
                    Flag : BOOLEAN;
                END Part;

                CLASS ClassA =
                    Parts : LIST OF Part;
                    MANDATORY CONSTRAINT Parts[FIRST]->Flag;
                END ClassA;

                CLASS ClassB =
                    Flag : BOOLEAN;
                END ClassB;

                ASSOCIATION Assoc =
                    RoleA -- {0..*} ClassA;
                    RoleB -- {1} ClassB;
                END Assoc;

                CONSTRAINTS OF ClassA =
                    MANDATORY CONSTRAINT RoleB[Assoc]->Flag;
                END;
            END Topic;
        END Model.
        """;

    /// <summary>
    /// Every name the path writes: its steps, plus the association qualifying a role — a written name with its own
    /// span and target that is not a step.
    /// </summary>
    private static IEnumerable<PathSegment> Occurrences(IReference reference)
        => reference.Path.SelectMany(segment => segment is RolePathSegment role ? new[] { segment, role.Association } : new[] { segment });

    /// <summary>Every name the path writes, one per line; see <see cref="Describe(PathSegment)"/>.</summary>
    private static string DescribeOccurrences(PathExpression path)
        => string.Join("\n", Occurrences(path.Reference).Select(Describe));

    /// <summary>The condition of the single mandatory constraint of <paramref name="container"/>, as a path.</summary>
    private static PathExpression ConstraintPath(IConstraintContainer container)
        => (PathExpression)container.Constraints.OfType<MandatoryConstraint>().Single().Condition!;

    private static (ClassDef ClassA, ConstraintsBlockDef Block) Parts(InterlisEnvironment environment)
    {
        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        return ((ClassDef)topic.Content["ClassA"], topic.Content.Values.OfType<ConstraintsBlockDef>().Single());
    }

    [Test]
    public async Task EveryStepCarriesItsOwnSpanAndTarget()
    {
        var environment = await ReadWithoutErrors(PathModel);
        var (classA, _) = Parts(environment);
        var path = ConstraintPath(classA);

        using (Assert.Multiple())
        {
            // Parts[FIRST]->Flag: an indexed step into a LIST OF substructure, then a member of that structure. The
            // intermediate step is named by nothing else, so only its own segment can carry it for a rename.
            await Assert.That(DescribeOccurrences(path)).IsEqualTo("""
                Parts[FIRST]@10:33-10:38 -> Model.Topic.ClassA -> Parts
                Flag@10:47-10:51 -> Model.Topic.Part -> Flag
                """.ReplaceLineEndings("\n"));

            // The path as a whole reaches its last step.
            await Assert.That(Describe(path.Reference)).IsEqualTo("Parts[FIRST]@10:33-10:38->Flag@10:47-10:51 -> Model.Topic.Part -> Flag");
            await Assert.That(path.Reference.Resolution).IsEqualTo<ReferenceResolution>(ReferenceResolution.ObjectPath);
        }
    }

    [Test]
    public async Task ARoleStepWritesTheRoleAndItsAssociation()
    {
        var environment = await ReadWithoutErrors(PathModel);
        var (_, block) = Parts(environment);

        // RoleB[Assoc]->Flag: the role and the association qualifying it are two separate names, each renameable;
        // the qualifier is an occurrence but not a step.
        await Assert.That(DescribeOccurrences(ConstraintPath(block))).IsEqualTo("""
            RoleB[Assoc]@23:33-23:38 -> Model.Topic.Assoc -> RoleB
            Assoc@23:39-23:44 -> Model.Topic.Assoc
            Flag@23:47-23:51 -> Model.Topic.ClassB -> Flag
            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task APathIsOneReferenceRegisteredOnTheContainerThatWritesIt()
    {
        var environment = await ReadWithoutErrors(PathModel);
        var (classA, block) = Parts(environment);

        using (Assert.Multiple())
        {
            // The inline constraint's path belongs to the class that writes it ...
            await Assert.That(classA.ContainerReferences).Contains(ConstraintPath(classA).Reference);

            // ... and a CONSTRAINTS OF block is its own container, so its path registers there, not on the target class.
            await Assert.That(block.ContainerReferences).Contains(ConstraintPath(block).Reference);
        }
    }

    [Test]
    public async Task AKeywordStepDenotesTheContextObjectNotADefinition()
    {
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        Flag : BOOLEAN;
                        MANDATORY CONSTRAINT THIS->Flag;
                    END ClassA;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var path = ConstraintPath((ClassDef)topic.Content["ClassA"]);

        using (Assert.Multiple())
        {
            // THIS is an occurrence with a span but no target: nothing to navigate to, nothing a rename may touch.
            await Assert.That(DescribeOccurrences(path)).IsEqualTo("""
                THIS@6:33-6:37 -> <unresolved>
                Flag@6:39-6:43 -> Model.Topic.ClassA -> Flag
                """.ReplaceLineEndings("\n"));
            await Assert.That(path.Reference.Path[0]).IsTypeOf<KeywordPathSegment>();
        }
    }

    [Test]
    public async Task APathEndingInThisReachesTheContextViewable()
    {
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        Flag : BOOLEAN;
                        MANDATORY CONSTRAINT DEFINED (THIS);
                    END ClassA;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var classA = (ClassDef)topic.Content["ClassA"];
        var path = (PathExpression)((DefinedExpression)classA.Constraints.OfType<MandatoryConstraint>().Single().Condition!).Operand;

        using (Assert.Multiple())
        {
            // The reference reaches the viewable — that is what the tip type is derived from — ...
            await Assert.That(path.Reference.Target).IsSameReferenceAs(classA);

            // ... but the keyword segment stays untargeted, so a rename of ClassA never rewrites the text THIS.
            await Assert.That(path.Reference.Path.Single().Target).IsNull();
        }
    }

    [Test]
    public async Task AViewKeywordDenotesTheObjectItsFormationImplies()
    {
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    STRUCTURE Part =
                        Flag : BOOLEAN;
                    END Part;

                    CLASS ClassA =
                        Name : TEXT*20;
                        Parts : LIST OF Part;
                        Geom : AREA WITH (STRAIGHTS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    END ClassA;

                    VIEW Elements
                        INSPECTION OF a~ClassA -> Parts;
                        WHERE DEFINED (PARENT->Name);
                        =
                    END Elements;

                    VIEW Boundaries
                        AREA INSPECTION OF a~ClassA -> Geom;
                        =
                        LeftName := THISAREA -> Name;
                    END Boundaries;

                    VIEW Groups
                        AGGREGATION OF a~ClassA ALL;
                        =
                        Objects := AGGREGATES;
                    END Groups;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var parent = (PathExpression)((DefinedExpression)((ViewDef)topic.Content["Elements"]).Selections.Single()).Operand;
        var thisArea = (PathExpression)((AttributeDef)((ViewDef)topic.Content["Boundaries"]).Content["LeftName"]).Values.Single();
        var aggregates = (PathExpression)((AttributeDef)((ViewDef)topic.Content["Groups"]).Content["Objects"]).Values.Single();

        using (Assert.Multiple())
        {
            // PARENT is the object the inspected Parts belong to, THISAREA an object of the inspected area partition,
            // AGGREGATES the aggregated objects: each a ClassA, so the walk continues into its members and the
            // expression gets a type. The keyword itself still denotes no definition.
            await Assert.That(Describe(parent.Reference)).IsEqualTo("PARENT@18:27-18:33->Name@18:35-18:39 -> Model.Topic.ClassA -> Name");
            await Assert.That(Describe(thisArea.Reference)).IsEqualTo("THISAREA@25:24-25:32->Name@25:36-25:40 -> Model.Topic.ClassA -> Name");
            await Assert.That(Describe(aggregates.Reference)).IsEqualTo("AGGREGATES@31:23-31:33 -> Model.Topic.ClassA");
            await Assert.That(parent.Reference.Path[0].Target).IsNull();
        }
    }

    [Test]
    public async Task AnInspectionBaseDenotesTheInspectedElementsNotTheViewable()
    {
        // The shape of LWB_Nutzungsflaechen_V2_0.Nutzung.InspectionOfProgramm: the base of an INSPECTION stands for
        // the inspected structure elements, so 'Nutzung_Programm->Reference' is a member of the element structure,
        // not of the viewable the elements are taken from (ili2c compiles the original model without complaint).
        var environment = await ReadWithoutErrors("""
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Katalog =
                        Gueltig_Von : 1900 .. 2100;
                    END Katalog;

                    STRUCTURE KatalogRef =
                        Reference : MANDATORY REFERENCE TO Katalog;
                    END KatalogRef;

                    CLASS Nutzung =
                        Bezugsjahr : 1900 .. 2100;
                        Programm : BAG {1..*} OF KatalogRef;
                    END Nutzung;

                    VIEW Programme
                        INSPECTION OF Nutzung_Programm ~ Nutzung -> Programm;
                        =
                        ALL OF Nutzung_Programm;
                        Bezugsjahr := PARENT->Bezugsjahr;
                        MANDATORY CONSTRAINT NOT (DEFINED (Nutzung_Programm->Reference->Gueltig_Von)) OR Nutzung_Programm->Reference->Gueltig_Von <= Bezugsjahr;
                    END Programme;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var paths = ((ViewDef)topic.Content["Programme"]).ContainerReferences
            .Where(reference => reference.Resolution == ReferenceResolution.ObjectPath && reference.Path.Count == 3)
            .ToList();

        using (Assert.Multiple())
        {
            await Assert.That(paths.Count).IsEqualTo(2);
            foreach (var path in paths)
            {
                await Assert.That(string.Join("\n", path.Path.Select(Describe).Select(line => line[(line.IndexOf(" -> ") + 4)..]))).IsEqualTo("""
                    Model.Topic.Programme.Nutzung_Programm
                    Model.Topic.KatalogRef -> Reference
                    Model.Topic.Katalog -> Gueltig_Von
                    """.ReplaceLineEndings("\n"));
            }
        }
    }
}
