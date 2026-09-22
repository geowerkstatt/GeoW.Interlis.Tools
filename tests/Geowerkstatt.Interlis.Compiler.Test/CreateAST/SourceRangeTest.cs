using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using static Geowerkstatt.Interlis.Compiler.TestTools;

namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// Verifies that every definition carries the source range of its whole declaration. These ranges are what an
/// editor needs to select or fold a definition, and the reference resolver derives the declaration ORDER of a
/// container's elements from them (a <see cref="Dictionary{TKey, TValue}"/> guarantees no enumeration order), so
/// the translation pairing depends on them being present and correct.
/// <para>
/// Asserted here rather than through the AST comparison test cases because those deliberately ignore a
/// definition's own range — it is uniform plumbing that would clutter every expectation (see
/// <see cref="TestTools"/>). This test therefore reads the ranges directly.
/// </para>
/// </summary>
public class SourceRangeTest
{
    [Test]
    public async Task DefinitionsCarryTheirDeclarationRange()
    {
        // Rendered like a diagnostic location (RangePosition.ToString): one-based lines, zero-based characters. Each
        // declaration ends at the character after its final token: 'END Model.' ends at character 10 of line 10, the
        // domain ';' at 22 of line 4, ...
        var environment = await ReadWithoutErrors(
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Kind = (a, b);
                TOPIC Topic =
                    CLASS ClassName =
                        Attr : TEXT*10;
                    END ClassName;
                END Topic;
            END Model.
            """);

        var model = (ModelDef)environment.Content["Model"];
        var domain = (DomainDef)model.Content["Kind"];
        var topic = (TopicDef)model.Content["Topic"];
        var classDef = (ClassDef)topic.Content["ClassName"];
        var attribute = (AttributeDef)classDef.Content["Attr"];

        using (Assert.Multiple())
        {
            await Assert.That(model.SourceRange?.ToString()).IsEqualTo("2:0-10:10");
            await Assert.That(domain.SourceRange?.ToString()).IsEqualTo("4:8-4:22");
            await Assert.That(topic.SourceRange?.ToString()).IsEqualTo("5:4-9:14");
            await Assert.That(classDef.SourceRange?.ToString()).IsEqualTo("6:8-8:22");
            await Assert.That(attribute.SourceRange?.ToString()).IsEqualTo("7:12-7:27");
        }
    }

    [Test]
    public async Task TopicContentRangesFollowTheDeclarationOrder()
    {
        // A CONSTRAINTS OF block may stand anywhere among a topic's contents, but the AST builder appends the
        // blocks after the other elements, so the order in which a topic's content dictionary was filled is NOT
        // the declaration order. The ranges must still reflect the source, so that ordering by them recovers the
        // declaration order.
        var environment = await ReadWithoutErrors(
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS First =
                        Attr : TEXT*10;
                    END First;

                    CONSTRAINTS OF First =
                        MANDATORY CONSTRAINT DEFINED (Attr);
                    END;

                    CLASS Last =
                    END Last;
                END Topic;
            END Model.
            """);

        var topic = (TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"];
        var byDeclaration = topic.Content.Values
            .OrderBy(element => element.SourceRange!.Start.Line)
            .Select(element => element.Name)
            .ToList();

        await Assert.That(byDeclaration).IsEquivalentTo(
            ["First", "CONSTRAINTS OF First #1", "Last"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task RangesCarryTheSourceUriOfTheirDocument()
    {
        var environment = await ReadWithoutErrors(
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                END Topic;
            END Model.
            """,
            "file:///model.ili");

        var model = (ModelDef)environment.Content["Model"];
        var topic = (TopicDef)model.Content["Topic"];

        using (Assert.Multiple())
        {
            await Assert.That(model.SourceRange?.SourceUri).IsEqualTo("file:///model.ili");
            await Assert.That(topic.SourceRange?.SourceUri).IsEqualTo("file:///model.ili");
            await Assert.That(topic.NameLocations.All(location => location.SourceUri == "file:///model.ili")).IsTrue();
        }
    }

    [Test]
    public async Task RangesOfSourcesWithoutUriHaveNoSourceUri()
    {
        var environment = await ReadWithoutErrors(
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
            END Model.
            """);

        await Assert.That(((ModelDef)environment.Content["Model"]).SourceRange?.SourceUri).IsNull();
    }

    [Test]
    public async Task ExpressionsCarryTheirRange()
    {
        // Every node of an expression tree covers exactly the text it was parsed from: the condition as a whole,
        // each operand, and the constants and paths at the leaves (line 6, counted one-based as diagnostics do).
        var environment = await ReadWithoutErrors(
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassName =
                        Attr : 0 .. 100;
                        MANDATORY CONSTRAINT Attr > 1 AND DEFINED (Attr);
                    END ClassName;
                END Topic;
            END Model.
            """);

        var classDef = (ClassDef)((TopicDef)((ModelDef)environment.Content["Model"]).Content["Topic"]).Content["ClassName"];
        var constraint = (MandatoryConstraint)classDef.Constraints.Single();
        var condition = (LogicalExpression)constraint.Condition;
        var comparison = (ComparisonExpression)condition.FirstOperand;
        var defined = (DefinedExpression)condition.SecondOperand;

        using (Assert.Multiple())
        {
            await Assert.That(condition.SourceRange?.ToString()).IsEqualTo("6:33-6:60");
            await Assert.That(comparison.SourceRange?.ToString()).IsEqualTo("6:33-6:41");
            await Assert.That(comparison.FirstOperand.SourceRange?.ToString()).IsEqualTo("6:33-6:37");
            await Assert.That(comparison.SecondOperand.SourceRange?.ToString()).IsEqualTo("6:40-6:41");
            await Assert.That(defined.SourceRange?.ToString()).IsEqualTo("6:46-6:60");
            await Assert.That(defined.Operand.SourceRange?.ToString()).IsEqualTo("6:55-6:59");
        }
    }
}
