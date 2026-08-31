using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A <c>MANDATORY CONSTRAINT</c>: the condition must hold for every object (RefHB 3.12).
/// </summary>
public sealed class MandatoryConstraint : ConstraintDef
{
    public required IExpression Condition { get; init; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitMandatoryConstraint(this);
    }
}
