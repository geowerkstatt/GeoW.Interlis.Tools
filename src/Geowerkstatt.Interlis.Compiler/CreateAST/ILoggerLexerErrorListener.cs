using Antlr4.Runtime;
using Geowerkstatt.Interlis.Compiler.AST;
using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

internal class ILoggerLexerErrorListener(ILoggerFactory loggerFactory) : IAntlrErrorListener<int>
{
    private readonly ILogger logger = loggerFactory.CreateLogger("Lexer");

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        // The lexer reports the offending character (ANTLR lines are one-based), so the range covers just that character.
        var range = new RangePosition(line - 1, charPositionInLine, line - 1, charPositionInLine + 1, recognizer.InputStream.ToSourceUri());
        logger.LogError(e, "Compile error at {Range} {Message}.", range, msg);
    }
}
