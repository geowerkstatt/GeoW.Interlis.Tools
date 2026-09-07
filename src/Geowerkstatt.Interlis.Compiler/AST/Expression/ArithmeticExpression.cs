using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// A binary arithmetic expression (<c>+</c>, <c>-</c>, <c>*</c>, <c>/</c>). The written operator is preserved
/// in <see cref="Operator"/> rather than being encoded by the node type, so a single node covers all four
/// arithmetic operators.
/// </summary>
public class ArithmeticExpression : BinaryExpression
{
    public ArithmeticExpression() : base(new NumericType())
    {
    }

    /// <summary>The arithmetic operator that was written.</summary>
    public required ArithmeticOperator Operator { get; init; }

    /// <summary>The binary arithmetic operators (RefHB 3.13).</summary>
    public enum ArithmeticOperator
    {
        Addition,
        Subtraction,
        Multiplication,
        Division,
    }
}
