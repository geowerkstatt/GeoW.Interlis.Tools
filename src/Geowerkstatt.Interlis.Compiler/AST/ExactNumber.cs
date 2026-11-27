using System.Globalization;
using System.Numerics;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An INTERLIS number held exactly as <see cref="Unscaled"/> × 10^-<see cref="Scale"/>, with its nearest double in
/// <see cref="Double"/>. RefHB 3.2.4-3 limits neither digits nor scaling, and a double alone loses digits:
/// 9007199254740993 and 9007199254740992 are the same double. Rounding preserves the order, so comparing the doubles
/// decides unless they are equal.
/// </summary>
/// <remarks>
/// <see cref="Unscaled"/> and <see cref="Scale"/> carry no trailing zeros, so every number has exactly one pair:
/// <c>1.0</c>, <c>1.00</c> and <c>0.1E1</c> are all 1 with scale 0.
/// </remarks>
public readonly struct ExactNumber : IEquatable<ExactNumber>, IComparable<ExactNumber>
{
    private ExactNumber(BigInteger unscaled, int scale, double value)
    {
        Unscaled = unscaled;
        Scale = scale;
        Double = value;
    }

    /// <summary>Creates the number <paramref name="unscaled"/> × 10^-<paramref name="scale"/>, e.g. 59.999 as (59999, 3).</summary>
    /// <exception cref="OverflowException">Stripping trailing zeros would push the scale below <see cref="int.MinValue"/>.</exception>
    public ExactNumber(BigInteger unscaled, int scale)
    {
        while (!unscaled.IsZero && (unscaled % 10).IsZero)
        {
            unscaled /= 10;
            scale = checked(scale - 1);
        }

        Unscaled = unscaled;
        Scale = unscaled.IsZero ? 0 : scale;
        Double = NearestDouble(Unscaled, Scale);
    }

    /// <summary>The digits of the number as an integer, without the decimal point and trailing zeros (<c>0.050</c> is 5).</summary>
    public BigInteger Unscaled { get; }

    /// <summary>The power of ten <see cref="Unscaled"/> is divided by; negative for a number scaled up by its exponent.</summary>
    public int Scale { get; }

    /// <summary>The nearest double; infinite or 0 for a number beyond the magnitude a double holds.</summary>
    public double Double { get; }

    /// <summary>
    /// Parses an INTERLIS number literal: an optional sign, digits with an optional fraction, and an optional exponent
    /// (<c>-12</c>, <c>0.050</c>, <c>0.123E-10</c>).
    /// </summary>
    /// <returns><see langword="false"/> for text that is not a number or an exponent beyond <see cref="int"/>.</returns>
    public static bool TryParse(string text, out ExactNumber number)
    {
        number = default;
        var exponentIndex = text.IndexOfAny(['e', 'E']);
        var exponent = 0;
        if (exponentIndex >= 0 && !int.TryParse(text.AsSpan(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
        {
            return false;
        }

        var mantissa = exponentIndex >= 0 ? text[..exponentIndex] : text;
        var pointIndex = mantissa.IndexOf('.');
        var fractionDigits = pointIndex >= 0 ? mantissa.Length - pointIndex - 1 : 0;
        var digits = pointIndex >= 0 ? mantissa.Remove(pointIndex, 1) : mantissa;
        var significant = digits.TrimEnd('0');
        var trailingZeros = digits.Length - significant.Length;
        if (significant is "" or "+" or "-")
        {
            significant = "0";
        }

        if (!BigInteger.TryParse(significant, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var unscaled)
            || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var scale = unscaled.IsZero ? 0 : (long)fractionDigits - trailingZeros - exponent;
        if (scale is < int.MinValue or > int.MaxValue)
        {
            return false;
        }

        number = new ExactNumber(unscaled, (int)scale, value);
        return true;
    }

    /// <inheritdoc cref="TryParse"/>
    /// <exception cref="FormatException">The text is not a number or its exponent is beyond <see cref="int"/>.</exception>
    public static ExactNumber Parse(string text)
        => TryParse(text, out var number) ? number : throw new FormatException($"'{text}' is not a number.");

    public static implicit operator ExactNumber(long value)
    {
        var (unscaled, scale) = (value, 0);
        while (unscaled != 0 && unscaled % 10 == 0)
        {
            unscaled /= 10;
            scale--;
        }

        return new ExactNumber(unscaled, scale, value);
    }

    /// <summary>
    /// The nearest double, ties to even: the 53-bit quotient |unscaled| / (10^scale · 2^shift) (fewer bits for a
    /// subnormal) is computed with integers, rounded by its remainder and scaled back by 2^shift.
    /// </summary>
    private static double NearestDouble(BigInteger unscaled, int scale)
    {
        // Far outside the double range (doubles span about 1E-324 to 1E308), the huge power of ten is not needed.
        var magnitude = BigInteger.Abs(unscaled).GetBitLength() * Math.Log10(2) - scale;
        if (magnitude is > 400 or < -400)
        {
            return unscaled.Sign * (magnitude > 0 ? double.PositiveInfinity : 0.0);
        }

        var numerator = BigInteger.Abs(unscaled) * BigInteger.Pow(10, Math.Max(-scale, 0));
        var denominator = BigInteger.Pow(10, Math.Max(scale, 0));
        var shift = (int)Math.Max(numerator.GetBitLength() - denominator.GetBitLength() - 53, -1074);
        (numerator, denominator) = shift < 0 ? (numerator << -shift, denominator) : (numerator, denominator << shift);
        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        if (quotient.GetBitLength() > 53)
        {
            // The bit-length estimate can be one too high: halve the quotient.
            shift++;
            denominator <<= 1;
            quotient = BigInteger.DivRem(numerator, denominator, out remainder);
        }

        var half = (remainder << 1).CompareTo(denominator);
        if (half > 0 || (half == 0 && !quotient.IsEven))
        {
            quotient++;
        }

        return unscaled.Sign * Math.ScaleB((double)quotient, shift);
    }

    public int CompareTo(ExactNumber other)
    {
        var sign = Unscaled.Sign.CompareTo(other.Unscaled.Sign);
        if (sign != 0 || Unscaled.IsZero)
        {
            return sign;
        }

        // Same sign: the order of magnitude decides first, so 1E300 against 1E-300 needs no 600-digit alignment.
        var magnitude = Magnitude.CompareTo(other.Magnitude);
        if (magnitude != 0)
        {
            return Unscaled.Sign * magnitude;
        }

        // Same magnitude: the scales differ by no more than the digit counts.
        var shift = Scale - other.Scale;
        return shift >= 0
            ? Unscaled.CompareTo(other.Unscaled * BigInteger.Pow(10, shift))
            : (Unscaled * BigInteger.Pow(10, -shift)).CompareTo(other.Unscaled);
    }

    /// <summary>The number of digits before the decimal point (negative for leading zeros after it): 10^(m-1) ≤ |x| &lt; 10^m.</summary>
    private long Magnitude => (long)Digits.Length - Scale;

    private string Digits => BigInteger.Abs(Unscaled).ToString(CultureInfo.InvariantCulture);

    public bool Equals(ExactNumber other) => Unscaled == other.Unscaled && Scale == other.Scale;

    public override bool Equals(object? obj) => obj is ExactNumber other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Unscaled, Scale);

    /// <summary>
    /// The number in decimal notation (<c>0.05</c>, <c>1200</c>), or in INTERLIS mantissa notation (<c>0.1E400</c>) where
    /// decimal notation would need more than 20 padding zeros. Trailing zeros of a literal are not kept.
    /// </summary>
    public override string ToString()
    {
        const int maxPaddingZeros = 20;
        var sign = Unscaled.Sign < 0 ? "-" : "";
        var digits = Digits;
        if (Scale <= 0 && -(long)Scale <= maxPaddingZeros)
        {
            return sign + digits + new string('0', -Scale);
        }

        if (Scale > 0 && Scale - digits.Length <= maxPaddingZeros)
        {
            var padded = digits.PadLeft(Scale + 1, '0');
            return $"{sign}{padded[..^Scale]}.{padded[^Scale..]}";
        }

        return $"{sign}0.{digits}E{(long)digits.Length - Scale}";
    }

    public static bool operator ==(ExactNumber left, ExactNumber right) => left.Equals(right);

    public static bool operator !=(ExactNumber left, ExactNumber right) => !left.Equals(right);

    public static bool operator <(ExactNumber left, ExactNumber right) => left.CompareTo(right) < 0;

    public static bool operator >(ExactNumber left, ExactNumber right) => left.CompareTo(right) > 0;

    public static bool operator <=(ExactNumber left, ExactNumber right) => left.CompareTo(right) <= 0;

    public static bool operator >=(ExactNumber left, ExactNumber right) => left.CompareTo(right) >= 0;
}
