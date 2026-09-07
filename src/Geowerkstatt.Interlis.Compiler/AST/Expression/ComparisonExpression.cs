using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// A binary comparison expression. The written operator is preserved in <see cref="Operator"/> instead of
/// being desugared into equality / greater-than / not combinations, so <c>&lt;</c> stays distinct from a
/// swapped <c>&gt;</c> and <c>&lt;&gt;</c> stays distinct from a negated <c>==</c>.
/// </summary>
public class ComparisonExpression : BinaryExpression
{
    public ComparisonExpression() : base(new BooleanType())
    {
    }

    /// <summary>The comparison operator that was written.</summary>
    public required ComparisonOperator Operator { get; init; }

    /// <summary>The binary comparison operators (RefHB 3.13): <c>==</c>, <c>&lt;&gt;</c>, <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&gt;=</c>.</summary>
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
    }
}
