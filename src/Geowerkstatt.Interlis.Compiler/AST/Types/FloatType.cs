namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A numeric range in mantissa (exponential) notation (RefHB 3.8.5-3): both bounds are written as
/// <c>0.mantissa E scaling</c>, and values are transferred in mantissa representation. Its resolution is relative —
/// <see cref="MantissaLength"/> counts the significant digits — so the range has <b>no uniform step</b>. (The
/// reference value tooling, iox-ili, nevertheless applies the mantissa length as decimal places when rounding
/// transfer values.)
/// </summary>
public class FloatType : NumericType
{
    /// <summary>
    /// The Stellenzahl: the number of mantissa digits of the bounds (ili2c's <c>accuracy</c>), e.g. 3 for
    /// <c>0.100E7</c>.
    /// </summary>
    public required int MantissaLength { get; init; }
}
