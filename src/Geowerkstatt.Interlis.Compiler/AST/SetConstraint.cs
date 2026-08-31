using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A <c>SET CONSTRAINT</c>: a condition evaluated over the whole set of objects (optionally per basket and
/// restricted by a <c>WHERE</c> pre-condition) (RefHB 3.12).
/// </summary>
public sealed class SetConstraint : ConstraintDef
{
    public bool IsBasket { get; set; }
    public IExpression? Where { get; set; }
    public required IExpression Condition { get; init; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitSetConstraint(this);
    }
}
