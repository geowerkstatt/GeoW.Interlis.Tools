using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// A binary logical expression: conjunction (<c>AND</c>), disjunction (<c>OR</c>) or implication (<c>=&gt;</c>).
/// The written operator is preserved in <see cref="Operator"/>; implication is kept as an operator rather than
/// being desugared into <c>OR(NOT a, b)</c>.
/// </summary>
public class LogicalExpression : BinaryExpression
{
    public LogicalExpression() : base(new BooleanType())
    {
    }

    /// <summary>The logical operator that was written.</summary>
    public required LogicalOperator Operator { get; init; }

    /// <summary>The binary logical operators (RefHB 3.13): <c>AND</c>, <c>OR</c>, <c>=&gt;</c>.</summary>
    public enum LogicalOperator
    {
        And,
        Or,
        Implication,
    }
}
