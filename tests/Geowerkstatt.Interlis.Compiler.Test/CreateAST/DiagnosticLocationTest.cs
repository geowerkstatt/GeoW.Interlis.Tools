using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.Test;
using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// Verifies what a diagnostics consumer relies on: every problem the compiler logs carries its
/// <see cref="RangePosition"/>, source URI included, as the <c>{Range}</c> value in the message state, and that range
/// is the one rendered in the message text.
/// </summary>
public class DiagnosticLocationTest
{
    private static (InterlisReader Reader, DiagnosticCollector Collector, ILoggerFactory LoggerFactory) CreateReader()
    {
        var collector = new DiagnosticCollector();
        var loggerFactory = LoggerFactory.Create(b => b.AddProvider(collector));
        return (new InterlisReader(loggerFactory), collector, loggerFactory);
    }

    private static IReadOnlyList<CollectedDiagnostic> Compile(string source, string sourceUri)
    {
        var (reader, collector, loggerFactory) = CreateReader();
        using (loggerFactory)
        {
            reader.ReadFile(new StringReader(source), sourceUri);
        }

        return collector.Diagnostics;
    }

    [Test]
    public async Task SyntaxErrorsAreLocatedInTheParsedSource()
    {
        var diagnostics = Compile(
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              CLASS
            END Model.
            """,
            "file:///broken.ili");

        await Assert.That(diagnostics.Count).IsGreaterThan(0);

        using (Assert.Multiple())
        {
            foreach (var diagnostic in diagnostics)
            {
                await Assert.That(diagnostic.Level).IsEqualTo<LogLevel>(LogLevel.Error);
                await Assert.That(diagnostic.Range?.SourceUri).IsEqualTo("file:///broken.ili");
                await Assert.That(diagnostic.Range).IsNotNull();

                // The range in the state is the one rendered in the message.
                await Assert.That(diagnostic.Message).Contains($" at {diagnostic.Range}");
            }
        }
    }

    [Test]
    public async Task UnresolvedReferencesAreLocatedAtTheReference()
    {
        var diagnostics = Compile(
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
                CLASS A =
                  Attr : Unknown;
                END A;
              END Topic;
            END Model.
            """,
            "file:///model.ili");

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        var diagnostic = diagnostics[0];

        using (Assert.Multiple())
        {
            await Assert.That(diagnostic.Level).IsEqualTo<LogLevel>(LogLevel.Error);
            await Assert.That(diagnostic.Message).IsEqualTo("Could not resolve 'reference 'Unknown' from Model.Topic.A' at 5:13-5:20");
            await Assert.That(diagnostic.Range?.SourceUri).IsEqualTo("file:///model.ili");
            await Assert.That(diagnostic.Range?.ToString()).IsEqualTo("5:13-5:20");
        }
    }

    [Test]
    public async Task TypeCheckErrorsAreLocatedAtTheRejectedDefinition()
    {
        var diagnostics = Compile(
            """
            INTERLIS 2.4;
            TYPE MODEL Model AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
              END Topic;
            END Model.
            """,
            "file:///model.ili");

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        var diagnostic = diagnostics[0];

        using (Assert.Multiple())
        {
            await Assert.That(diagnostic.Level).IsEqualTo<LogLevel>(LogLevel.Error);
            await Assert.That(diagnostic.Message).IsEqualTo("Type check error in 'Model.Topic' at 3:2-4:12: a topic can not be part of a TYPE model.");
            await Assert.That(diagnostic.Range?.SourceUri).IsEqualTo("file:///model.ili");
            await Assert.That(diagnostic.Range?.ToString()).IsEqualTo("3:2-4:12");
        }
    }

    [Test]
    public async Task ProblemsInImportedModelsCarryTheImportedSource()
    {
        var (reader, collector, loggerFactory) = CreateReader();
        using (loggerFactory)
        {
            var resolver = new DelegateModelResolver(name => name == "Dep"
                ? (new StringReader(
                    """
                    INTERLIS 2.4;
                    MODEL Dep AT "http://example.com" VERSION "1.0.0" =
                      DOMAIN
                        Kind EXTENDS Missing = (a, b);
                    END Dep.
                    """), "file:///dep.ili")
                : null);

            await reader.ReadModelWithImportsAsync(
                new StringReader(
                    """
                    INTERLIS 2.4;
                    MODEL Root AT "http://example.com" VERSION "1.0.0" =
                      IMPORTS Dep;
                      DOMAIN
                        Own = (x, y);
                    END Root.
                    """),
                resolver,
                "file:///root.ili");
        }

        var diagnostics = collector.Diagnostics;
        await Assert.That(diagnostics).Count().IsEqualTo(1);

        using (Assert.Multiple())
        {
            await Assert.That(diagnostics[0].Message).Contains("Could not resolve");
            await Assert.That(diagnostics[0].Message).Contains("Missing");
            await Assert.That(diagnostics[0].Range?.SourceUri).IsEqualTo("file:///dep.ili");
            await Assert.That(diagnostics[0].Range?.ToString()).IsEqualTo("4:17-4:24");
        }
    }

    [Test]
    public async Task PathProblemsAreLocatedAtThePath()
    {
        var diagnostics = Compile(
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
                CLASS A =
                  Attr : TEXT*10;
                  UNIQUE Missing;
                END A;
              END Topic;
            END Model.
            """,
            "file:///model.ili");

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        var diagnostic = diagnostics[0];

        using (Assert.Multiple())
        {
            await Assert.That(diagnostic.Level).IsEqualTo<LogLevel>(LogLevel.Error);
            await Assert.That(diagnostic.Message).IsEqualTo("Could not resolve 'Missing' in 'Model.Topic.A' at 6:13-6:20");
            await Assert.That(diagnostic.Range?.SourceUri).IsEqualTo("file:///model.ili");
            await Assert.That(diagnostic.Range?.ToString()).IsEqualTo("6:13-6:20");
        }
    }

    [Test]
    public async Task ImportedModelOfAnotherVersionIsReportedAtTheImport()
    {
        var (reader, collector, loggerFactory) = CreateReader();
        using (loggerFactory)
        {
            var resolver = new DelegateModelResolver(name => name == "Dep"
                ? (new StringReader(
                    """
                    INTERLIS 2.3;
                    MODEL Dep AT "http://example.com" VERSION "1.0.0" =
                    END Dep.
                    """), "file:///dep.ili")
                : null);

            await reader.ReadModelWithImportsAsync(
                new StringReader(
                    """
                    INTERLIS 2.4;
                    MODEL Root AT "http://example.com" VERSION "1.0.0" =
                      IMPORTS Dep;
                    END Root.
                    """),
                resolver,
                "file:///root.ili");
        }

        var mismatch = collector.Diagnostics.Single(d => d.Message.StartsWith("Imported model", StringComparison.Ordinal));
        using (Assert.Multiple())
        {
            await Assert.That(mismatch.Level).IsEqualTo<LogLevel>(LogLevel.Error);
            await Assert.That(mismatch.Message).IsEqualTo("Imported model 'Dep' at 3:10-3:13 has INTERLIS version 2.3, expected 2.4.");
            await Assert.That(mismatch.Range?.SourceUri).IsEqualTo("file:///root.ili");

            // The model is not merged, so the import itself stays unresolved.
            await Assert.That(collector.Diagnostics.Any(d => d.Message.StartsWith("Could not resolve 'reference 'Dep'", StringComparison.Ordinal))).IsTrue();
        }
    }

    [Test]
    public async Task ReadRuleAttributesProblemsToTheGivenSource()
    {
        // A caller that parses through ReadRule (the language server) passes the document URI along with the text.
        var (reader, collector, loggerFactory) = CreateReader();
        using (loggerFactory)
        {
            reader.ReadRule(
                new StringReader("INTERLIS 2.4; MODEL Model AT \"http://example.com\" VERSION \"1\" = TOPIC"),
                (parser, visitor) => visitor.VisitInterlis(parser.interlis()),
                sourceUri: "file:///document.ili");
        }

        await Assert.That(collector.Diagnostics.Count).IsGreaterThan(0);
        using (Assert.Multiple())
        {
            foreach (var diagnostic in collector.Diagnostics)
            {
                await Assert.That(diagnostic.Range?.SourceUri).IsEqualTo("file:///document.ili");
            }
        }
    }

    private sealed class DelegateModelResolver(Func<string, (TextReader Reader, string? SourceUri)?> open) : IModelResolver
    {
        public ValueTask<(TextReader Reader, string? SourceUri)?> OpenModelAsync(string modelName, double? languageVersion, CancellationToken cancellationToken) => new(open(modelName));
    }
}
