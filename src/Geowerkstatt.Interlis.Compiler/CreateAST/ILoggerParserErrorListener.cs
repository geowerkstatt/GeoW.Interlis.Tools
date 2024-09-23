using Antlr4.Runtime;
using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Tools.CreateAST;

internal class ILoggerParserErrorListener(ILoggerFactory loggerFactory) : IAntlrErrorListener<IToken>
{
    private readonly ILogger logger = loggerFactory.CreateLogger("Parser");

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        logger.LogError(e, "Compile error at line {Line}:{CharPosition} {Message}.", line, charPositionInLine, msg);
    }
}
