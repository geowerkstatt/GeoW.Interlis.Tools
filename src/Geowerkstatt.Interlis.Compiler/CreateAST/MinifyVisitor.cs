using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System.Text;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

public class MinifyVisitor : Interlis24ParserBaseVisitor<MinifyVisitor.Part>
{
    protected override Part DefaultResult => Part.Empty;

    public override Part VisitTerminal(ITerminalNode node)
    {
        return node.Symbol.Type switch
        {
            TokenConstants.EOF => new Part(string.Empty, node.Symbol, node.Symbol),
            _ => new Part(node.Symbol.Text, node.Symbol, node.Symbol),
        };
    }

    public override Part VisitErrorNode(IErrorNode node)
    {
        return VisitTerminal(node);
    }

    protected override Part AggregateResult(Part aggregate, Part nextResult)
    {
        if (aggregate == Part.Empty)
        {
            return nextResult;
        }
        else if (nextResult == Part.Empty)
        {
            return aggregate;
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append(aggregate.Content);
            if (aggregate.StopToken != null && nextResult.StartToken != null)
            {
                AppendDefault(sb, aggregate.StopToken, nextResult.StartToken);
            }

            sb.Append(nextResult.Content);

            return new Part(sb.ToString(), aggregate.StartToken, nextResult.StopToken);
        }
    }

    private void AppendDefault(StringBuilder sb, IToken lastToken, IToken nextToken)
    {
        var nextChar = nextToken.Text.First();
        var previousChar = lastToken.Text.Last();

        if (IsIdentifierChar(previousChar) && IsIdentifierChar(nextChar))
        {
            sb.Append(' ');
        }
    }

    private bool IsIdentifierChar(char c)
    {
        return char.IsLetter(c) || char.IsNumber(c) || c == '_';
    }

    private Part FormatChildren(IEnumerable<IParseTree> children)
    {
        Part accu = Part.Empty;
        foreach (var child in children)
        {
            accu = AggregateResult(accu, Visit(child));
        }

        return accu;
    }

    public override Part VisitString([NotNull] Interlis24Parser.StringContext context)
    {
        return new Part(context.Start.TokenSource.InputStream.GetText(new Interval(context.Start.StartIndex, context.Stop.StopIndex)), context.Start, context.Stop);
    }

    public override Part VisitModelDef([NotNull] Interlis24Parser.ModelDefContext context)
    {
        if (context.IMPORTS().Length > 0)
        {
            // rewrite imports
            int startIndex = context.children.IndexOf(context.IMPORTS().First());
            int stopIndex = context.children.IndexOf(context.SEMICOLON().Last());

            var sb = new StringBuilder();
            var before = FormatChildren(context.children.Take(startIndex + 1));
            var after = FormatChildren(context.children.Skip(stopIndex));

            sb.Append(before.Content);
            sb.Append(' ');
            sb.Append(Visit(context.modelImport().First()).Content);
            foreach (var import in context.modelImport().Skip(1))
            {
                sb.Append(',');
                sb.Append(Visit(import).Content);
            }

            sb.Append(after.Content);
            return new Part(sb.ToString(), context.Start, context.Stop);
        }
        else
        {
            return base.VisitModelDef(context);
        }
    }

    public record Part(string Content, IToken? StartToken, IToken? StopToken)
    {
        public static readonly Part Empty = new Part(string.Empty, null, null);
    }
}
