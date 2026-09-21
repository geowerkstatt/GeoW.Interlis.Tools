namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// The reference-system link of a numeric type or coordinate axis (RefHB 3.8.5-19, RefHB 3.10.3): the referenced
/// system (<see cref="Value"/>) — either the <c>&lt;...&gt;</c> form referencing a coordinate domain or the
/// <c>{...}</c> form referencing a declared meta object — with an optional axis index (legal in both forms per
/// RefHB 3.8.5-19).
/// </summary>
public class RefSys
{
    /// <summary>
    /// The referenced system: a <see cref="CoordDomainRef"/> (<c>&lt;coord&gt;</c>) or a
    /// <see cref="MetaObjectRef"/> (<c>{metaObject}</c>). Only <see langword="null"/> while the reference is
    /// still being typed and neither form's content exists yet (e.g. a lone <c>&lt;</c> or <c>{</c>).
    /// </summary>
    public Target? Value { get; set; }

    /// <summary>
    /// The optional axis index (<c>[n]</c>).
    /// </summary>
    public int? Axis { get; set; }

    /// <summary>
    /// The system a <see cref="RefSys"/> references; the two forms are mutually exclusive by construction.
    /// </summary>
    public abstract class Target
    {
    }

    /// <summary>
    /// The <c>&lt;coord&gt;</c> form referencing a coordinate domain.
    /// </summary>
    public sealed class CoordDomainRef : Target
    {
        /// <summary>
        /// The referenced coordinate domain. Typed to <see cref="DomainDef"/>, so the resolver only accepts a
        /// domain target — anything else stays unresolved and is reported.
        /// </summary>
        public required Reference<DomainDef> Domain { get; init; }
    }

    /// <summary>
    /// The <c>{[basket.]metaObject}</c> form referencing a meta object declared by a basket.
    /// </summary>
    public sealed class MetaObjectRef : Target
    {
        /// <summary>
        /// The basket the <c>{basket.metaObject}</c> form qualifies its meta object with (the
        /// <c>MetaDataBasketRef</c> prefix of RefHB 3.10.1-8), or <see langword="null"/> for an unqualified
        /// meta-object name.
        /// </summary>
        public Reference<MetaDataBasketDef>? Basket { get; init; }

        /// <summary>
        /// The meta-object name. A meta object is not a model definition — it is a NAME declared by a basket
        /// (RefHB 3.10.1-2) — so the reference resolves as <see cref="ReferenceResolution.Member"/>: the scoped
        /// resolver leaves it alone and the reference resolver writes its target, the
        /// matching <see cref="MetaObjectDeclaration"/> of the basket or one it extends, searched in the runtime
        /// order (RefHB 3.10.1-3). The target stays <see langword="null"/> for an unqualified name, an unresolved
        /// basket, or a name the basket chain does not declare (which the type checker reports). The reference
        /// itself is <see langword="null"/> only while the name is still being typed.
        /// </summary>
        public Reference<MetaObjectDeclaration>? MetaObject { get; init; }
    }
}
