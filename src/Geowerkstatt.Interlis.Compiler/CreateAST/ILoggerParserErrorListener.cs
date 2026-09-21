using Antlr4.Runtime;
using Geowerkstatt.Interlis.Compiler.AST;
using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

internal class ILoggerParserErrorListener(ILoggerFactory loggerFactory) : IAntlrErrorListener<IToken>
{
    private readonly ILogger logger = loggerFactory.CreateLogger("Parser");

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var range = offendingSymbol?.ToRange() ?? new RangePosition(line - 1, charPositionInLine, line - 1, charPositionInLine + 1, recognizer.InputStream.ToSourceUri());
        logger.LogError(e, "Compile error at {Range} {Message}.", range, msg);
    }
}
