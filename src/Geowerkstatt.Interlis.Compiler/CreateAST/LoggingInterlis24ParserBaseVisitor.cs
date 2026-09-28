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
        logger.LogWarning("Rule '{Name}' at {Range} not implemented.", name, token.ToRange());
        return default!;
    }

    public override TResult VisitChildren(IRuleNode node)
    {
        if (node.RuleContext is not ParserRuleContext context)
        {
            return base.VisitChildren(node);
        }

        // A node that failed to parse (e.g. no viable alternative of a rule with labeled alternatives) is the
        // generic rule context, not one of the alternatives a visitor implements. The syntax error has been
        // reported already, so there is nothing missing in the visitor to warn about.
        if (context.exception != null)
        {
            return default!;
        }

        return LogNotImplementedWarning(context);
    }

    /// <summary>
    /// Calls the base <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/> method without logging a warning.
    /// Sometimes a rule intentionally just visits its children.
    protected TResult VisitChildrenBase(IRuleNode node)
    {
        return base.VisitChildren(node);
    }
}
