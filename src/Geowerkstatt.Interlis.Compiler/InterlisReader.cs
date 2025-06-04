using Antlr4.Runtime;
using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools.CreateAST;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Geowerkstatt.Interlis.Tools;

public class InterlisReader
{
    private readonly ILoggerFactory loggerFactory;

    public InterlisReader(ILoggerFactory? loggerFactory = null)
    {
        this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Compiles the content of the <paramref name="textReader"/> to a <see cref="InterlisFile"/>.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <returns>The compiled representation of the <paramref name="textReader"/> input.</returns>
    public InterlisFile ReadFile(TextReader textReader)
    {
        var (interlisFile, unresolvedReferences) = ReadRule(textReader, (p, v) => v.VisitInterlis(p.interlis()));

        var referenceResolver = new Interlis24AstReferenceResolverVisitor(loggerFactory, unresolvedReferences);
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
    public (TResult, List<IUnresolvedReference>) ReadRule<TResult>(TextReader textReader, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule, int lineOffset = 0)
    {
        var inputStream = CharStreams.fromTextReader(textReader);

        var interlisLexer = new Interlis24Lexer(inputStream);
        interlisLexer.TokenFactory = new LineOffsetDecorator(interlisLexer.TokenFactory, lineOffset);
        interlisLexer.RemoveErrorListeners();
        interlisLexer.AddErrorListener(new ILoggerLexerErrorListener(loggerFactory));

        var tokenStream = new CommonTokenStream(interlisLexer);
        var interlisParser = new Interlis24Parser(tokenStream);
        interlisParser.RemoveErrorListeners();
        interlisParser.AddErrorListener(new ILoggerParserErrorListener(loggerFactory));

        var visitor = new Interlis24Visitor(loggerFactory, tokenStream);
        return (parseRule(interlisParser, visitor), visitor.ReferencesToResolve);
    }
}
