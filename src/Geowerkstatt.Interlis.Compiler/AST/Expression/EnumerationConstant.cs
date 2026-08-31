using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// An enumeration constant (RefHB 3.13-1: <c>'#' (name {'.' name} ['.' OTHERS] | 'OTHERS')</c>), naming an
/// enumeration element by its dotted path. <c>OTHERS</c> stands for the potential further values an extension may
/// still add below the named node — or anywhere in the enumeration for a bare <c>#OTHERS</c>. It is usable in
/// expressions, function arguments and sign assignments, but is not a valid stored value (RefHB 3.8.3).
/// </summary>
public class EnumerationConstant : ConstantExpression
{
    /// <summary>The named enumeration element path; empty for a bare <c>#OTHERS</c>.</summary>
    public List<string> Path { get; } = new List<string>();

    /// <summary>Whether the constant ends in <c>OTHERS</c> (<c>#a.b.OTHERS</c>, or <c>#OTHERS</c> with an empty <see cref="Path"/>).</summary>
    public bool IsOthers { get; init; }

    public EnumerationConstant() : base(new EnumerationType())
    {
    }
}
