namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// Base of the numeric range types (RefHB 3.8.5). The notation of the bounds is part of the type: a
/// <see cref="DecimalType"/> is written in decimal notation and spans a uniform value grid, a
/// <see cref="FloatType"/> is written in mantissa (exponential) notation and declares significant digits instead of
/// a step. A plain <see cref="NumericType"/> (no subtype) declares neither bounds nor notation: the bound-less
/// <c>NUMERIC</c> and the unspecified numeric result type of arithmetic expressions.
/// </summary>
public class NumericType : TypeDef
{
    public double? Min { get; set; }
    public double? Max { get; set; }

    public bool Circular { get; set; }
    public Reference<UnitDef>? Unit { get; set; }

    /// <summary>
    /// The rotation orientation of an angular value (<c>CLOCKWISE</c> / <c>COUNTERCLOCKWISE</c>, RefHB 3.8.5/3.10.3).
    /// </summary>
    public AngleOrientation Orientation { get; set; }

    /// <summary>
    /// The reference system this numeric value (coordinate axis) refers to, if any (RefHB 3.8.5/3.10.3).
    /// </summary>
    public RefSys? RefSystem { get; set; }

    /// <summary>
    /// A numeric extension inherits every definition part it omits (RefHB 3.8-4): a bound-less <c>NUMERIC</c>
    /// keeps the effective base's bounds, precision and notation; an extension with its own bounds replaces them
    /// (the range grid comes with the bounds, so bounds and precision travel together); unit, reference system,
    /// rotation orientation and <c>CIRCULAR</c> are inherited when unwritten. A non-numeric effective base is a
    /// kind clash — the authored extension is kept and the type checker reports it.
    /// </summary>
    internal override TypeDef MergeWithBase(TypeDef effectiveBase)
    {
        if (effectiveBase is not NumericType numericBase)
        {
            return this;
        }

        // The side that declares bounds contributes the range facets; a bound-less NUMERIC (a plain NumericType,
        // no notation subtype) contributes none. Both sides bound-less merges into a bound-less result.
        var range = this is DecimalType or FloatType ? this : numericBase;
        NumericType merged = range switch
        {
            FloatType floatRange => new FloatType { MantissaLength = floatRange.MantissaLength },
            DecimalType decimalRange => new DecimalType { Precision = decimalRange.Precision },
            _ => new NumericType(),
        };

        merged.Min = range.Min;
        merged.Max = range.Max;
        merged.Circular = Circular || numericBase.Circular;
        merged.Unit = Unit ?? numericBase.Unit;
        merged.Orientation = Orientation != AngleOrientation.None ? Orientation : numericBase.Orientation;
        merged.RefSystem = RefSystem ?? numericBase.RefSystem;
        return merged;
    }

    public enum AngleOrientation
    {
        None = 0,
        Clockwise,
        CounterClockwise,
    }
}
