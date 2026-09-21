using Antlr4.Runtime;
using Geowerkstatt.Interlis.Compiler.AST;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Converts ANTLR tokens and rule contexts to the (zero-based) <see cref="RangePosition"/>s the AST and the
/// diagnostics use, and looks up the ranges of AST elements. ANTLR lines are one-based, columns zero-based. The
/// <see cref="RangePosition.SourceUri"/> is the <see cref="IIntStream.SourceName"/> of the stream the tokens were
/// read from (see <see cref="InterlisReader.RunLexer"/>).
/// </summary>
internal static class RangeExtensions
{
    /// <summary>
    /// The range the <paramref name="token"/> covers.
    /// </summary>
    /// <remarks>Only works correctly if the <paramref name="token"/> does not span multiple lines.</remarks>
    public static RangePosition ToRange(this IToken token) => token.ToRange(token);

    /// <summary>
    /// The range the <paramref name="context"/> covers, from its first to its last token.
    /// </summary>
    /// <remarks>Only works correctly if the last token does not span multiple lines.</remarks>
    public static RangePosition ToRange(this ParserRuleContext context) => context.Start.ToRange(context.Stop);

    /// <summary>
    /// The range spanning from the <paramref name="start"/> token to the end of the <paramref name="stop"/> token.
    /// The end is computed on the stop token's start line, so a stop token spanning several lines (a multi-line
    /// string) gets an approximate end; the <c>EOF</c> token, which has no text of its own, counts as one character.
    /// </summary>
    /// <remarks>Only works correctly if the <paramref name="stop"/> token does not span multiple lines.</remarks>
    public static RangePosition ToRange(this IToken start, IToken stop)
    {
        var stopLength = stop.Type == TokenConstants.EOF ? 1 : Math.Max(1, stop.Text?.Length ?? 0);
        return new RangePosition(start.Line - 1, start.Column, stop.Line - 1, stop.Column + stopLength, start.InputStream.ToSourceUri());
    }

    /// <summary>
    /// The source URI a <paramref name="stream"/> was opened with, or <see langword="null"/> for an unnamed stream
    /// (ANTLR reports <see cref="IntStreamConstants.UnknownSourceName"/> for those).
    /// </summary>
    public static string? ToSourceUri(this IIntStream? stream)
    {
        var name = stream?.SourceName;
        return string.IsNullOrEmpty(name) || name == IntStreamConstants.UnknownSourceName ? null : name;
    }

    /// <summary>
    /// The <see cref="ISourceRange.SourceRange"/> of <paramref name="element"/> or, if it has none, of its nearest
    /// ancestor that has one; <see langword="null"/> if no ancestor has a range.
    /// </summary>
    public static RangePosition? GetNearestSourceRange(this IInterlisDefinition? element)
    {
        while (element != null)
        {
            if (element.SourceRange != null)
            {
                return element.SourceRange;
            }

            element = element.Parent;
        }

        return null;
    }

    /// <summary>
    /// The range of <paramref name="reference"/> or, if it recorded none (a reference built during error recovery),
    /// that of the definition it is written in.
    /// </summary>
    public static RangePosition? GetRange(this IReference reference) => reference.SourceRange ?? reference.Source.GetNearestSourceRange();
}
