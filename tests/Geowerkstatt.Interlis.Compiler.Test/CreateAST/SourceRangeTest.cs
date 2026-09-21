using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.Test;
using Microsoft.Extensions.Logging;

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
    /// <summary>
    /// Parses <paramref name="source"/> through the full pipeline and asserts that no diagnostics were reported,
    /// so a test can rely on the returned AST being complete.
    /// </summary>
    private static async Task<InterlisEnvironment> ReadWithoutErrors(string source, string? sourceUri = null)
    {
        var logProvider = new TestLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(logProvider));
        var environment = new InterlisReader(loggerFactory).ReadFile(new StringReader(source), sourceUri);

        await Assert.That(logProvider.GetMessages()).IsEquivalentTo(Array.Empty<string>());
        return environment;
    }

    /// <summary>Renders a range as <c>startLine:startCharacter..endLine:endCharacter</c> for readable assertions.</summary>
    private static string Describe(ISourceRange element)
    {
        var range = element.SourceRange;
        return range == null
            ? "<none>"
            : $"{range.Start.Line}:{range.Start.Character}..{range.End.Line}:{range.End.Character}";
    }

    [Test]
    public async Task DefinitionsCarryTheirDeclarationRange()
    {
        // Lines are zero-based, so MODEL is line 1 and DOMAIN is line 2. Each declaration ends at the character
        // after its final token: 'END Model.' ends at character 10 of line 9, the domain ';' at 24 of line 3, ...
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
            await Assert.That(Describe(model)).IsEqualTo("1:0..9:10");
            await Assert.That(Describe(domain)).IsEqualTo("3:8..3:22");
            await Assert.That(Describe(topic)).IsEqualTo("4:4..8:14");
            await Assert.That(Describe(classDef)).IsEqualTo("5:8..7:22");
            await Assert.That(Describe(attribute)).IsEqualTo("6:12..6:27");
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
        // each operand, and the constants and paths at the leaves (line 6, zero-based).
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
            await Assert.That(Describe(condition)).IsEqualTo("5:33..5:60");
            await Assert.That(Describe(comparison)).IsEqualTo("5:33..5:41");
            await Assert.That(Describe(comparison.FirstOperand)).IsEqualTo("5:33..5:37");
            await Assert.That(Describe(comparison.SecondOperand)).IsEqualTo("5:40..5:41");
            await Assert.That(Describe(defined)).IsEqualTo("5:46..5:60");
            await Assert.That(Describe(defined.Operand)).IsEqualTo("5:55..5:59");
        }
    }
}
