namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A value out of an enumeration, characterized on two axes — WHICH enumeration it comes from
/// (<see cref="TargetEnumeration"/>) and whether only leaf values are admitted (<see cref="LeafsOnly"/>):
/// <list type="bullet">
/// <item><c>ALL OF X</c> (RefHB 3.8.3) — <see cref="TargetEnumeration"/> = X, <see cref="LeafsOnly"/> =
/// <see langword="false"/>: every node and leaf of the referenced enumeration; usable wherever a base type is.</item>
/// <item><c>ENUMTREEVAL</c> (RefHB 3.14-8) — no target, <see cref="LeafsOnly"/> = <see langword="false"/>: any
/// value of any enumeration tree; function arguments only.</item>
/// <item><c>ENUMVAL</c> (RefHB 3.14-7) — no target, <see cref="LeafsOnly"/> = <see langword="true"/>: a leaf
/// value of any enumeration; function arguments only.</item>
/// </list>
/// The fourth combination — a target with <see cref="LeafsOnly"/> — has no source form: "the leaves of X" is what
/// a plain domain reference to X denotes (a <see cref="TypeRef"/> alias), so it is never built from source.
/// Distinct from <see cref="EnumerationType"/>, the authored enumeration definition carrying its values.
/// </summary>
public class EnumerationValuesType : TypeDef
{
    /// <summary>The enumeration the value comes from; <see langword="null"/> = any enumeration (<c>ENUMVAL</c>/<c>ENUMTREEVAL</c>).</summary>
    public Reference<DomainDef>? TargetEnumeration { get; set; }

    /// <summary>Whether only leaf values are admitted rather than tree node values too.</summary>
    public required bool LeafsOnly { get; init; }
}
