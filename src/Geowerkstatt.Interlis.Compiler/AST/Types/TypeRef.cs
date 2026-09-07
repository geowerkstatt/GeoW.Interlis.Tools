namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A type that references (extends) another type without changing anything.
/// </summary>
public class TypeRef : TypeDef
{
    /// <summary>A pure alias changes nothing, so the effective type of the base chain passes through.</summary>
    internal override TypeDef MergeWithBase(TypeDef effectiveBase) => effectiveBase;
}
