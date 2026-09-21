using Antlr4.Runtime;
using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.CreateAST;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Geowerkstatt.Interlis.Compiler;

public class InterlisReader
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;

    public InterlisReader(ILoggerFactory? loggerFactory = null)
    {
        this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        logger = this.loggerFactory.CreateLogger<InterlisReader>();
    }

    /// <summary>
    /// Compiles the content of the <paramref name="textReader"/> to a <see cref="InterlisEnvironment"/>.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <param name="sourceUri">The filepath or URL to the source interlis file.</param>
    /// <returns>The compiled representation of the <paramref name="textReader"/> input.</returns>
    public InterlisEnvironment ReadFile(TextReader textReader, string? sourceUri = null)
    {
        var interlisFile = ParseModels(textReader, sourceUri);
        ResolveAndCheck(interlisFile);
        return interlisFile;
    }

    /// <summary>
    /// Compiles the <paramref name="rootReader"/> together with its transitive imported models. Every imported model
    /// that is not already loaded is fetched through <paramref name="modelResolver"/>, parsed, and merged into a
    /// single <see cref="InterlisEnvironment"/>; references are then resolved and the type checker runs once over the
    /// complete environment. Imports the resolver cannot supply are left unresolved and reported as compile errors
    /// during name resolution; a supplied model of another INTERLIS version than the root is reported at the import
    /// and not merged.
    /// </summary>
    /// <param name="rootReader">The input to compile.</param>
    /// <param name="modelResolver">Supplies the source of imported models on demand.</param>
    /// <param name="sourceUri">The filepath or URL to the root source interlis file.</param>
    /// <param name="cancellationToken">Cancels the compilation between resolving imported models.</param>
    /// <returns>The compiled representation of the <paramref name="rootReader"/> input and its loaded dependencies.</returns>
    public async Task<InterlisEnvironment> ReadModelWithImportsAsync(TextReader rootReader, IModelResolver modelResolver, string? sourceUri = null, CancellationToken cancellationToken = default)
    {
        var root = ParseModels(rootReader, sourceUri);
        var environment = new InterlisEnvironment { Version = root.Version };
        environment.MergeFrom(root);

        // Load required models until the environment is closed under dependencies. Each model name is requested at
        // most once, so unavailable or cyclic dependencies terminate the loop instead of looping forever.
        var requestedModels = new HashSet<string>(StringComparer.Ordinal);
        bool addedModels;
        do
        {
            addedModels = false;
            var missingModels = environment.Content.Values
                .SelectMany(GetDependencies)
                .Where(dependency => !environment.Content.ContainsKey(dependency.ModelName) && requestedModels.Add(dependency.ModelName))
                .ToList();

            foreach (var (modelName, reference) in missingModels)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await modelResolver.OpenModelAsync(modelName, environment.Version, cancellationToken) is not { } modelSource)
                {
                    continue;
                }

                InterlisEnvironment imported;
                using (modelSource.Reader)
                {
                    imported = ParseModels(modelSource.Reader, modelSource.SourceUri);
                }

                try
                {
                    environment.MergeFrom(imported);
                }
                catch (InvalidOperationException)
                {
                    // A model of another INTERLIS version: the import stays unresolved (which the reference resolver
                    // reports) and the reason is given here.
                    logger.LogError("Imported model '{ModelName}' at {Range} has INTERLIS version {ImportedVersion}, expected {Version}.", modelName, reference.GetRange(), imported.Version, environment.Version);
                    continue;
                }

                addedModels = true;
            }
        }
        while (addedModels);

        ResolveAndCheck(environment);
        return environment;
    }

    /// <summary>
    /// The external models a <paramref name="model"/> depends on and that must therefore be loaded into the
    /// environment for its references to resolve: its imported models plus, for a translation, the base-language model
    /// named by its <c>TRANSLATION OF</c> clause (RefHB 3.5.1-10). The base model is not imported, so it would
    /// otherwise never be requested. Each dependency comes with the reference that names it, for locating problems.
    /// </summary>
    private static IEnumerable<(string ModelName, IReference Reference)> GetDependencies(ModelDef model)
    {
        foreach (var (importName, import) in model.Imports)
        {
            yield return (importName, import.ModelDef);
        }

        if (model.TranslationOf is { Path: { Count: > 0 } translationPath } translationOf)
        {
            yield return (translationPath[0], translationOf);
        }
    }

    /// <summary>
    /// Parses the <paramref name="textReader"/> into an <see cref="InterlisEnvironment"/> (including the internal
    /// INTERLIS model), without resolving references. The <see cref="ModelDef.SourceUri"/> of the parsed models is
    /// set to <paramref name="sourceUri"/>, as is the <see cref="RangePosition.SourceUri"/> of every range in them.
    /// </summary>
    private InterlisEnvironment ParseModels(TextReader textReader, string? sourceUri)
    {
        var interlisFile = ReadRule(textReader, (p, v) => v.VisitInterlis(p.interlis()), sourceUri: sourceUri);
        foreach (var model in interlisFile.Content.Values)
        {
            if (model != InternalModel.Interlis)
            {
                model.SourceUri = sourceUri;
            }
        }

        return interlisFile;
    }

    /// <summary>Resolves references and runs the type checker over the given <paramref name="environment"/> in place.</summary>
    private void ResolveAndCheck(InterlisEnvironment environment)
    {
        var referenceResolver = new Interlis24AstReferenceResolverVisitor(loggerFactory);
        environment.Accept(referenceResolver);

        // Resolve object/attribute paths after references (it descends through resolved targets) and before the type
        // checker (whose constraint-condition check reads the resolved path tip type).
        var pathResolver = new Interlis24AstPathResolverVisitor(loggerFactory);
        environment.Accept(pathResolver);

        var typeChecker = new Interlis24AstTypeCheckerVisitor(loggerFactory);
        environment.Accept(typeChecker);
    }

    /// <summary>
    /// Compiles the content of the <paramref name="textReader"/> using the <paramref name="parseRule"/> function.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <param name="parseRule">A function to parse the input given the <see cref="Interlis24Parser"/> and <see cref="Interlis24Visitor"/>.</param>
    /// <param name="lineOffset">Optional line number offset to get correct positions in error messages when only part of a file is parsed.</param>
    /// <param name="sourceUri">Optional filepath or URL of the source, recorded as the <see cref="RangePosition.SourceUri"/> of every range in the result and in the logged problems.</param>
    /// <returns>The compiled representation of the <paramref name="textReader"/> input.</returns>
    public TResult ReadRule<TResult>(TextReader textReader, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule, int lineOffset = 0, string? sourceUri = null)
    {
        var tokenStream = RunLexer(textReader, lineOffset, sourceUri);
        var interlisParser = GetParser(tokenStream);
        var astCreator = new Interlis24Visitor(loggerFactory, tokenStream);
        var result = parseRule(interlisParser, astCreator);

        // Require the whole input to be consumed so leftover tokens are reported instead of silently ignored.
        if (interlisParser.CurrentToken.Type != Interlis24Parser.Eof)
        {
            interlisParser.NotifyErrorListeners($"extraneous input '{interlisParser.CurrentToken.Text}' expecting <EOF>");
        }

        return result;
    }

    /// <summary>
    /// Create a <see cref="CommonTokenStream"/> from the given input <paramref name="textReader"/>.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <param name="lineOffset">Optional line number offset to get correct positions in error messages when only part of a file is parsed.</param>
    /// <param name="sourceUri">Optional filepath or URL of the source; becomes the stream's <see cref="IIntStream.SourceName"/> and thereby the <see cref="RangePosition.SourceUri"/> of the ranges built from its tokens.</param>
    /// <returns>A <see cref="CommonTokenStream"/> with the tokens from the lexer.</returns>
    public CommonTokenStream RunLexer(TextReader textReader, int lineOffset = 0, string? sourceUri = null)
    {
        var inputStream = CharStreams.fromTextReader(textReader);
        if (sourceUri != null && inputStream is BaseInputCharStream namedStream)
        {
            namedStream.name = sourceUri;
        }


        var interlisLexer = new Interlis24Lexer(inputStream);
        interlisLexer.TokenFactory = new LineOffsetDecorator(interlisLexer.TokenFactory, lineOffset);
        interlisLexer.RemoveErrorListeners();
        interlisLexer.AddErrorListener(new ILoggerLexerErrorListener(loggerFactory));

        var tokenStream = new CommonTokenStream(interlisLexer);

        // Run the lexer
        tokenStream.Fill();

        return tokenStream;
    }

    /// <summary>
    /// Configure a new instance of <see cref="Interlis24Parser"/> with the given <paramref name="tokenStream"/>.
    /// </summary>
    /// <param name="tokenStream">The <see cref="CommonTokenStream"/> to parse.</param>
    /// <returns>A <see cref="Interlis24Parser"/> for parsing.</returns>
    public Interlis24Parser GetParser(CommonTokenStream tokenStream)
    {
        var interlisParser = new Interlis24Parser(tokenStream);
        interlisParser.RemoveErrorListeners();
        interlisParser.AddErrorListener(new ILoggerParserErrorListener(loggerFactory));

        return interlisParser;
    }
}
