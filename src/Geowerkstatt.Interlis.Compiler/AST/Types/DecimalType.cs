namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A numeric range in decimal notation (RefHB 3.8.5): the digit count of the bounds implies a uniform value grid
/// with step <c>10^Precision</c>. A <c>NUMERIC</c> without bounds declares no notation and is a plain
/// <see cref="NumericType"/>, not a <see cref="DecimalType"/>.
/// </summary>
public class DecimalType : NumericType
{
    /// <summary>
    /// The power-of-ten exponent of the range's step (<c>0.00 .. 7.99</c> -> -2), i.e. the negative Stellenzahl
    /// (digit count after the decimal point).
    /// </summary>
    public required int Precision { get; init; }
}
