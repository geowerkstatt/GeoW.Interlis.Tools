using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Geowerkstatt.Interlis.Tools.CreateAST;

/// <summary>
/// This exception indicates that an assumption about the grammar was wrong.
/// Possibly because the grammar changed in unexpected ways.
/// </summary>
public class UnexpectedNodeException : Exception
{
    public UnexpectedNodeException(IParseTree OffendingNode) : this(OffendingNode, string.Empty)
    {
    }

    public UnexpectedNodeException(IToken OffendingToken) : this(OffendingToken, string.Empty)
    {
    }

    public UnexpectedNodeException(IParseTree OffendingNode, string Message)
        : base($"{CreateMessage(OffendingNode)}. {Message}")
    {
    }

    public UnexpectedNodeException(IToken OffendingToken, string Message)
        : base($"{CreateMessage(OffendingToken)}. {Message}")
    {
    }

    private static string CreateMessage(IParseTree OffendingNode)
    {
        return OffendingNode switch
        {
            ITerminalNode terminal => CreateMessage(terminal.Symbol),
            ParserRuleContext parserRule => $"Unexpected {parserRule.GetType().Name} at line {parserRule.Start.Line}:{parserRule.Start.Column} '{parserRule.GetText()}'",
            _ => $"Unexpected Node <{OffendingNode}>",
        };
    }

    private static string CreateMessage(IToken offendingToken)
        => $"Unexpected Token <{offendingToken.Type}> at line {offendingToken.Line}:{offendingToken.Column} '{offendingToken.Text}'";
}
