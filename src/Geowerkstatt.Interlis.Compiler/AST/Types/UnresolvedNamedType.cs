namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A reference to a named type in attribute or argument position (<c>attr : Foo</c>), before it is classified.
/// The grammar merges the domain, structure and class reference forms into a single rule (RefHB 3.6.1), so which
/// kind <c>Foo</c> denotes can only be known once its <see cref="Target"/> is resolved. The reference resolver then
/// rewrites the owning attribute's type in place: a <see cref="DomainDef"/> target becomes a <see cref="TypeRef"/>
/// alias (the same representation a domain definition uses for a domain reference), and a structure or
/// <c>ANYSTRUCTURE</c> target becomes a contained-substructure <see cref="ObjectType"/>.
/// <para>
/// An <see cref="UnresolvedNamedType"/> therefore persists only for targets that could not be classified: an unresolved
/// reference, a function-argument type (function arguments are not resolved, RefHB 3.14), or a rule-level parse
/// without reference resolution. The invalid uses — <c>ANYCLASS</c>, a restricted domain reference, and a class or
/// association without <c>REFERENCE TO</c> — also remain and are reported by the type checker (RefHB 3.6.1-12).
/// </para>
/// </summary>
public class UnresolvedNamedType : TypeDef
{
    /// <summary>The referenced named type (a domain, a structure, <c>ANYSTRUCTURE</c>, or — invalidly — a class or <c>ANYCLASS</c>), optionally with <c>RESTRICTION (...)</c>.</summary>
    public required RestrictedRef Target { get; init; }
}
