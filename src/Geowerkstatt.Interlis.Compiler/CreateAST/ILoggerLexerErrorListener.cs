using Antlr4.Runtime;
using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

internal class ILoggerLexerErrorListener(ILoggerFactory loggerFactory) : IAntlrErrorListener<int>
{
    private readonly ILogger logger = loggerFactory.CreateLogger("Lexer");

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        logger.LogError(e, "Compile error at line {Line}:{CharPosition} {Message}.", line, charPositionInLine, msg);
    }
}
