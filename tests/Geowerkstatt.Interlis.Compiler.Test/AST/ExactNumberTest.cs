using Geowerkstatt.Interlis.Compiler.AST;
using System.Numerics;

namespace Geowerkstatt.Interlis.Compiler;

public class ExactNumberTest
{
    [Test]
    [Arguments("0.050", 5, 2, 0.05, "0.05")]
    [Arguments("-12", -12, 0, -12.0, "-12")]
    [Arguments("+7", 7, 0, 7.0, "7")]
    [Arguments("1200", 12, -2, 1200.0, "1200")]
    [Arguments("1.00", 1, 0, 1.0, "1")]
    [Arguments("-0.0", 0, 0, 0.0, "0")]
    [Arguments("0.000E5", 0, 0, 0.0, "0")]
    [Arguments("0.123E10", 123, -7, 1230000000.0, "1230000000")]
    [Arguments("0.5E-20", 5, 21, 5E-21, "0.000000000000000000005")]
    [Arguments("0.1E400", 1, -399, double.PositiveInfinity, "0.1E400")]
    [Arguments("-0.123E-400", -123, 403, -0.0, "-0.123E-400")]
    [Arguments("9007199254740993", 9007199254740993, 0, 9007199254740992.0, "9007199254740993")]
    public async Task ParseStripsTrailingZeros(string text, long unscaled, int scale, double nearestDouble, string formatted)
    {
        var number = ExactNumber.Parse(text);

        await Assert.That(number.Unscaled).IsEqualTo(new BigInteger(unscaled));
        await Assert.That(number.Scale).IsEqualTo(scale);
        await Assert.That(number.Double).IsEqualTo(nearestDouble);
        await Assert.That(number.ToString()).IsEqualTo(formatted);
    }

    [Test]
    [Arguments("59999", 3, "59.999")]
    [Arguments("12000", 0, "12000")]
    [Arguments("-5", 25, "-0.5E-24")]
    [Arguments("9999999999999999999999999999999", 15, "9999999999999999.999999999999999")]
    [Arguments("0", 7, "0")]
    [Arguments("9007199254740993", 0, "9007199254740993")]
    [Arguments("9007199254740995", 0, "9007199254740995")]
    [Arguments("17976931348623157", -292, "0.17976931348623157E309")]
    [Arguments("17976931348623159", -292, "0.17976931348623159E309")]
    [Arguments("5", 324, "0.5E-323")]
    [Arguments("247", 326, "0.247E-323")]
    [Arguments("-1", -500, "-0.1E501")]
    [Arguments("1", 500, "0.1E-499")]
    public async Task ConstructsTheSameNumberAsParse(string unscaled, int scale, string text)
    {
        var number = new ExactNumber(BigInteger.Parse(unscaled), scale);
        var parsed = ExactNumber.Parse(text);

        await Assert.That(number).IsEqualTo(parsed);
        await Assert.That(number.Double).IsEqualTo(parsed.Double);
    }

    [Test]
    [Arguments("9007199254740993", "9007199254740992", 1, DisplayName = "Numbers with the same nearest double")]
    [Arguments("1.0", "1.00", 0, DisplayName = "Trailing zeros")]
    [Arguments("12.5", "0.125E2", 0, DisplayName = "Decimal and exponent notation")]
    [Arguments("0", "-0.0", 0, DisplayName = "Signed zero")]
    [Arguments("0.99", "1", -1, DisplayName = "Fraction against integer")]
    [Arguments("-2", "-10", 1, DisplayName = "Negative numbers")]
    [Arguments("0.1E300", "0.1E-300", 1, DisplayName = "Far apart magnitudes")]
    [Arguments("-0.1E300", "0.1E-300", -1, DisplayName = "Opposite signs")]
    public async Task ComparesExactly(string left, string right, int expected)
    {
        var (a, b) = (ExactNumber.Parse(left), ExactNumber.Parse(right));

        await Assert.That(Math.Sign(a.CompareTo(b))).IsEqualTo(expected);
        await Assert.That(Math.Sign(b.CompareTo(a))).IsEqualTo(-expected);
        await Assert.That(a == b).IsEqualTo(expected == 0);
    }

    [Test]
    public async Task EqualNumbersHaveEqualHashCodes()
    {
        var hashCodes = new[] { "1.0", "1.00", "0.1E1", "1" }.Select(text => ExactNumber.Parse(text).GetHashCode());

        await Assert.That(hashCodes.Distinct().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task ConstructsTheNearestDoubleLikeDoubleParse()
    {
        var random = new Random(42);
        for (var i = 0; i < 10000; i++)
        {
            var digits = string.Concat(Enumerable.Range(0, random.Next(1, 40)).Select(_ => (char)('0' + random.Next(10))));
            var scale = random.Next(-340, 340);
            var expected = double.Parse($"{digits}E{-scale}", System.Globalization.CultureInfo.InvariantCulture);

            await Assert.That(new ExactNumber(BigInteger.Parse(digits), scale).Double).IsEqualTo(expected);
        }
    }

    [Test]
    [Arguments(1200L, 12, -2, "1200")]
    [Arguments(-7L, -7, 0, "-7")]
    [Arguments(0L, 0, 0, "0")]
    [Arguments(9007199254740993L, 9007199254740993, 0, "9007199254740993")]
    public async Task ConvertsALong(long value, long unscaled, int scale, string text)
    {
        ExactNumber number = value;

        await Assert.That(number.Unscaled).IsEqualTo(new BigInteger(unscaled));
        await Assert.That(number.Scale).IsEqualTo(scale);
        await Assert.That(number.Double).IsEqualTo((double)value);
        await Assert.That(number.ToString()).IsEqualTo(text);
        await Assert.That(number).IsEqualTo(ExactNumber.Parse(text));
    }

    [Test]
    public async Task DefaultIsZero()
    {
        await Assert.That(default(ExactNumber)).IsEqualTo(ExactNumber.Parse("0"));
        await Assert.That(default(ExactNumber).ToString()).IsEqualTo("0");
    }

    [Test]
    public async Task RejectsAnExponentBeyondInt()
    {
        await Assert.That(ExactNumber.TryParse("0.1E99999999999", out _)).IsFalse();
    }

    [Test]
    public async Task HandlesTheSmallestScale()
    {
        var number = ExactNumber.Parse("10E2147483647");

        await Assert.That(number.Scale).IsEqualTo(int.MinValue);
        await Assert.That(number.ToString()).IsEqualTo("0.1E2147483649");
        await Assert.That(() => new ExactNumber(10, int.MinValue)).Throws<OverflowException>();
    }
}
