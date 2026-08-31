using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// A numeric constant (RefHB 3.13 <c>numericConst</c>). Its <see cref="Value"/> is either a numeric literal
/// (<see cref="Number"/>) or a predefined mathematical constant (<see cref="MathematicalConstant"/>), the latter
/// kept symbolic rather than folded to its <see cref="double"/> approximation so the AST reflects the input.
/// An optional <see cref="Unit"/> may qualify the value.
/// </summary>
public class NumericConstant : ConstantExpression
{
    /// <summary>The value of the constant: a numeric literal or a predefined mathematical constant.</summary>
    public required NumericValue Value { get; init; }

    public Reference<UnitDef>? Unit { get; init; }

    public NumericConstant() : base(new NumericType())
    {
    }

    /// <summary>
    /// The value of a <see cref="NumericConstant"/>: a numeric literal (<see cref="Number"/>) or a predefined
    /// mathematical constant (<see cref="MathematicalConstant"/>, RefHB 3.13).
    /// </summary>
    public abstract class NumericValue
    {
        /// <summary>A numeric literal is written directly; e.g. <c>Value = 42</c> stands for <c>new Number { Value = 42 }</c>.</summary>
        public static implicit operator NumericValue(double value) => new Number { Value = value };

        /// <summary>A predefined constant is written directly; e.g. <c>Value = PredefinedConstant.Pi</c> stands for <c>new MathematicalConstant { Kind = PredefinedConstant.Pi }</c>.</summary>
        public static implicit operator NumericValue(PredefinedConstant constant) => new MathematicalConstant { Kind = constant };
    }

    /// <summary>A numeric literal value.</summary>
    public sealed class Number : NumericValue
    {
        public required double Value { get; init; }
    }

    /// <summary>A predefined mathematical constant (RefHB 3.13): <c>PI</c> or <c>LNBASE</c>.</summary>
    public sealed class MathematicalConstant : NumericValue
    {
        /// <summary>Which predefined mathematical constant this is.</summary>
        public required PredefinedConstant Kind { get; init; }
    }

    /// <summary>The predefined mathematical constants (RefHB 3.13).</summary>
    public enum PredefinedConstant
    {
        /// <summary>The circle constant π, written <c>PI</c>.</summary>
        Pi,

        /// <summary>The base of the natural logarithm e, written <c>LNBASE</c>.</summary>
        LnBase,
    }
}
