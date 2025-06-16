using Antlr4.Runtime;
using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.CreateAST;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Geowerkstatt.Interlis.Compiler;

public class InterlisReader
{
    private readonly ILoggerFactory loggerFactory;

    public InterlisReader(ILoggerFactory? loggerFactory = null)
    {
        this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Compiles the content of the <paramref name="textReader"/> to a <see cref="InterlisEnvironment"/>.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <param name="sourceUri">The filepath or URL to the source interlis file.</param>
    /// <returns>The compiled representation of the <paramref name="textReader"/> input.</returns>
    public InterlisEnvironment ReadFile(TextReader textReader, string? sourceUri = null)
    {
        var interlisFile = ReadRule(textReader, (p, v) => v.VisitInterlis(p.interlis()));
        foreach (var model in interlisFile.Content.Values)
        {
            model.SourceUri = sourceUri;
        }

        var referenceResolver = new Interlis24AstReferenceResolverVisitor(loggerFactory);
        interlisFile.Accept(referenceResolver);

        return interlisFile;
    }

    /// <summary>
    /// Compiles the content of the <paramref name="textReader"/> using the <paramref name="parseRule"/> function.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <param name="parseRule">A function to parse the input given the <see cref="Interlis24Parser"/> and <see cref="Interlis24Visitor"/>.</param>
    /// <param name="lineOffset">Optional line number offset to get correct positions in error messages when only part of a file is parsed.</param>
    /// <returns>The compiled representation of the <paramref name="textReader"/> input and a list of <see cref="UnresolvedReference"/>s.</returns>
    public TResult ReadRule<TResult>(TextReader textReader, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule, int lineOffset = 0)
    {
        var tokenStream = RunLexer(textReader, lineOffset);
        var interlisParser = GetParser(tokenStream);
        var astCreator = new Interlis24Visitor(loggerFactory, tokenStream);
        return parseRule(interlisParser, astCreator);
    }

    /// <summary>
    /// Create a <see cref="CommonTokenStream"/> from the given input <paramref name="textReader"/>.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <param name="lineOffset">Optional line number offset to get correct positions in error messages when only part of a file is parsed.</param>
    /// <returns>A <see cref="CommonTokenStream"/> with the tokens from the lexer.</returns>
    public CommonTokenStream RunLexer(TextReader textReader, int lineOffset = 0)
    {
        var inputStream = CharStreams.fromTextReader(textReader);

        var interlisLexer = new Interlis24Lexer(inputStream);
        interlisLexer.TokenFactory = new LineOffsetDecorator(interlisLexer.TokenFactory, lineOffset);
        interlisLexer.RemoveErrorListeners();
        interlisLexer.AddErrorListener(new ILoggerLexerErrorListener(loggerFactory));

        return new CommonTokenStream(interlisLexer);
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
