namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// Instance(s) of viewable(s). The position decides the reading:
/// <list type="bullet">
/// <item>As an attribute type it is AUTHORED and CONTAINED: a substructure-valued attribute (<c>attr : Struct</c>,
/// <c>BAG OF</c> / <c>LIST OF Struct</c>, or <c>ANYSTRUCTURE</c>; RefHB 3.6.4) whose instance(s) the attribute
/// owns, navigable into their members. Produced by the reference resolver from an
/// <see cref="UnresolvedNamedType"/> whose target resolves to a structure (or <c>ANYSTRUCTURE</c>) — always with
/// exactly one entry in <see cref="Targets"/>. Distinct from <see cref="ReferenceType"/> (a <c>REFERENCE TO</c>
/// pointer to an independent object) and <see cref="TypeRef"/> (a transparent alias to a named domain).</item>
/// <item>As an expression <see cref="Expression.IExpression.ReturnType"/> or an <c>OBJECT</c>/<c>OBJECTS OF</c>
/// function argument it is COMPUTED and DENOTED: it describes which independently existing object(s) the
/// expression or argument stands for.</item>
/// </list>
/// <see cref="TypeDef.Cardinality"/> is the size of the owned collection or denoted set (<c>OBJECT</c> is
/// <c>{1..1}</c>, <c>OBJECTS</c> is <c>{0..*}</c>).
/// </summary>
public class ObjectType : TypeDef
{
    /// <summary>
    /// The viewable(s) the instance is statically known to belong to, each optionally narrowed with
    /// <c>RESTRICTION (...)</c>. Multiple entries occur for a multi-target association role; an object of such a type
    /// belongs to one of them. In computed positions the list is empty when nothing is known (<c>THIS</c> /
    /// <c>PARENT</c> and other keyword path tips, <c>ALL</c> without a restriction) — consumers must treat an empty
    /// list as "unknown" and skip, not report, such objects.
    /// </summary>
    public IReadOnlyList<RestrictedRef> Targets { get; init; } = [];
}
