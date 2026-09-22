using System.Globalization;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An index selecting a single element of an ordered sub-structure (<c>LIST OF</c>) or a coordinate axis — the
/// <c>[ FIRST | LAST | PosNumber ]</c> bracket of an <see cref="IndexedPathSegment"/>: a positional keyword
/// (<see cref="Keyword"/>) or a 1-based number (<see cref="Number"/>).
/// </summary>
public abstract class PathIndex
{
    /// <summary>A number index is written directly; e.g. <c>5</c> stands for <c>new Number { Value = 5 }</c>.</summary>
    public static implicit operator PathIndex(int value) => new Number { Value = value };

    /// <summary>A positional keyword is written directly; e.g. <c>IndexKeyword.First</c> stands for <c>new Keyword { Kind = IndexKeyword.First }</c>.</summary>
    public static implicit operator PathIndex(IndexKeyword keyword) => new Keyword { Kind = keyword };

    /// <summary>A 1-based list-element index or coordinate-axis number (<c>[ n ]</c>).</summary>
    public sealed class Number : PathIndex
    {
        public required int Value { get; init; }

        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A positional keyword index: the first (<c>[ FIRST ]</c>) or last (<c>[ LAST ]</c>) element of an ordered sub-structure.</summary>
    public sealed class Keyword : PathIndex
    {
        /// <summary>Which positional keyword was written.</summary>
        public required IndexKeyword Kind { get; init; }

        public override string ToString() => Kind.ToString().ToUpperInvariant();
    }
}

/// <summary>The positional index keywords (RefHB 3.13).</summary>
public enum IndexKeyword
{
    /// <summary>The first element of an ordered sub-structure (<c>[ FIRST ]</c>).</summary>
    First,

    /// <summary>The last element of an ordered sub-structure (<c>[ LAST ]</c>).</summary>
    Last,
}
