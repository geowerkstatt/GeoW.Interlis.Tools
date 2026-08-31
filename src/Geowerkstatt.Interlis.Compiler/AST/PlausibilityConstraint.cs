using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A plausibility constraint (<c>CONSTRAINT &lt;=|&gt;= Percentage % Condition</c>): the condition must hold for
/// at most / at least the given percentage of objects (RefHB 3.12).
/// </summary>
public sealed class PlausibilityConstraint : ConstraintDef
{
    public required Comparison Direction { get; init; }
    public required double Percentage { get; init; }
    public required IExpression Condition { get; init; }

    public enum Comparison
    {
        /// <summary><c>&lt;=</c> — the condition holds for at most the given percentage.</summary>
        AtMost,

        /// <summary><c>&gt;=</c> — the condition holds for at least the given percentage.</summary>
        AtLeast,
    }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitPlausibilityConstraint(this);
    }
}
