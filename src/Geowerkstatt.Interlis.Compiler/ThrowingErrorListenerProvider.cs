using Antlr4.Runtime.Misc;
using Antlr4.Runtime;

namespace Geowerkstatt.Interlis.Tools;

public class ThrowingErrorListenerProvider : IErrorListenerProvider
{
    public IAntlrErrorListener<T> GetErrorListener<T>() => new ThrowingErrorListener<T>();
}

/// <summary>
/// Antlr error listener that throws a <see cref="ParseCanceledException"/> if it receives a syntax error.
/// </summary>
public class ThrowingErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, TSymbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new ParseCanceledException($"Compile error at line {line}:{charPositionInLine} {msg}.", e);
    }
}
