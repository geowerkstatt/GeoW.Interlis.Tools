using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// A token factory decorator that adds the ability to offset the line number of tokens.
/// </summary>
internal sealed class LineOffsetDecorator(ITokenFactory baseFactory, int lineOffset) : ITokenFactory
{
    public int LineOffset { get; set; } = lineOffset;

    [return: NotNull]
    public IToken Create(Tuple<ITokenSource, ICharStream> source, int type, string text, int channel, int start, int stop, int line, int charPositionInLine)
    {
        return baseFactory.Create(source, type, text, channel, start, stop, line + LineOffset, charPositionInLine);
    }

    [return: NotNull]
    public IToken Create(int type, string text)
    {
        return baseFactory.Create(type, text);
    }
}
