namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// An indexed attribute step of an object path (<c>Attribute-Name '[' FIRST | LAST | PosNumber ']'</c>, RefHB 3.13).
/// The index is mandatory: its presence proves the step denotes an attribute (an ordered <c>LIST OF</c> sub-structure
/// or a coordinate), so — unlike a bare <see cref="IdentifierPathElement"/>, which could resolve to a role, base name
/// or reference attribute — this element is unambiguously an attribute. A non-indexed attribute is an
/// <see cref="IdentifierPathElement"/>.
/// </summary>
public sealed class AttributePathElement : IPathElement
{
    public required string Name { get; init; }

    public required PathIndex Index { get; init; }

    /// <summary>
    /// An index selecting a single element of an ordered sub-structure (<c>LIST OF</c>) or a coordinate axis
    /// (the <c>[ FIRST | LAST | PosNumber ]</c> bracket): a positional keyword (<see cref="KeywordIndex"/>) or a
    /// 1-based number (<see cref="NumberIndex"/>). Only meaningful on an <see cref="AttributePathElement"/>.
    /// </summary>
    public abstract class PathIndex
    {
        /// <summary>A number index is written directly; e.g. <c>Index = 5</c> stands for <c>new NumberIndex { Value = 5 }</c>.</summary>
        public static implicit operator PathIndex(int value) => new NumberIndex { Value = value };

        /// <summary>A positional keyword is written directly; e.g. <c>Index = IndexKeyword.First</c> stands for <c>new KeywordIndex { Kind = IndexKeyword.First }</c>.</summary>
        public static implicit operator PathIndex(IndexKeyword keyword) => new KeywordIndex { Kind = keyword };
    }

    /// <summary>A 1-based list-element index or coordinate-axis number (<c>[ n ]</c>).</summary>
    public sealed class NumberIndex : PathIndex
    {
        public required int Value { get; init; }
    }

    /// <summary>A positional keyword index: the first (<c>[ FIRST ]</c>) or last (<c>[ LAST ]</c>) element of an ordered sub-structure.</summary>
    public sealed class KeywordIndex : PathIndex
    {
        /// <summary>Which positional keyword was written.</summary>
        public required IndexKeyword Kind { get; init; }
    }

    /// <summary>The positional index keywords (RefHB 3.13).</summary>
    public enum IndexKeyword
    {
        /// <summary>The first element of an ordered sub-structure (<c>[ FIRST ]</c>).</summary>
        First,

        /// <summary>The last element of an ordered sub-structure (<c>[ LAST ]</c>).</summary>
        Last,
    }
}
