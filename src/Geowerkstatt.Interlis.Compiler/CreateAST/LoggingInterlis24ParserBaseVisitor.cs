using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Base visitor that logs a warning in all rule visit methods.
/// </summary>
public class LoggingInterlis24ParserBaseVisitor<TResult>(ILoggerFactory loggerFactory) : Interlis24ParserBaseVisitor<TResult>
{
    private readonly ILogger logger = loggerFactory.CreateLogger<LoggingInterlis24ParserBaseVisitor<TResult>>();

    private TResult LogNotImplementedWarning(ParserRuleContext context)
    {
        var name = Interlis24Parser.ruleNames[context.RuleIndex];
        var token = context.Start;
        logger.LogWarning("Rule '{Name}' at line {Line}:{Column} not implemented.", name, token.Line, token.Column);
        return default!;
    }

    public override TResult VisitChildren(IRuleNode node)
    {
        if (node.RuleContext is ParserRuleContext context)
        {
            return LogNotImplementedWarning(context);
        }
        else
        {
            return base.VisitChildren(node);
        }
    }

    /// <summary>
    /// Calls the base <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/> method without logging a warning.
    /// Sometimes a rule intentionally just visits its children.
    protected TResult VisitChildrenBase(IRuleNode node)
    {
        return base.VisitChildren(node);
    }
}
