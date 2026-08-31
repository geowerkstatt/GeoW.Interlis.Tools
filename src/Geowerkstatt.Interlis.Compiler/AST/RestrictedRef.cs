namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A reference to a class, structure, association or domain, optionally narrowed with <c>RESTRICTION (...)</c>.
/// The <see cref="Value"/> is either an explicit reference (<see cref="DefinitionRef"/>) or one of the <c>ANY…</c>
/// placeholders (<see cref="AnyRef"/>). The grammar collapses <c>DomainRef</c>, <c>RestrictedStructureRef</c> and
/// <c>RestrictedClassOrAssRef</c> into one rule that permits either form in every position, so which kind each
/// context actually allows is validated later by the type checker (RefHB 3.6.1-13/-15/-17).
/// </summary>
public class RestrictedRef
{
    /// <summary>The referenced target: an explicit reference or an <c>ANYCLASS</c> / <c>ANYSTRUCTURE</c> placeholder.</summary>
    public required RefTarget Value { get; init; }

    /// <summary>The allowed targets listed in <c>RESTRICTION (...)</c>, if any.</summary>
    public List<Reference<IInterlisDefinition>> Restrictions { get; } = new List<Reference<IInterlisDefinition>>();

    /// <summary>
    /// The target of a <see cref="RestrictedRef"/>: either an explicit <see cref="DefinitionRef"/> or an
    /// <see cref="AnyRef"/> placeholder. The two are mutually exclusive by construction.
    /// </summary>
    public abstract class RefTarget
    {
        /// <summary>An explicit reference is written directly; e.g. <c>Value = someReference</c> stands for <c>new DefinitionRef { Reference = someReference }</c>.</summary>
        public static implicit operator RefTarget(Reference<IInterlisDefinition> reference) => new DefinitionRef { Reference = reference };

        /// <summary>An <c>ANY…</c> placeholder is written directly; e.g. <c>Value = AnyKind.Structure</c> stands for <c>new AnyRef { Kind = AnyKind.Structure }</c>.</summary>
        public static implicit operator RefTarget(AnyKind kind) => new AnyRef { Kind = kind };
    }

    /// <summary>An explicit reference to a class, association, structure or domain (RefHB 3.5.3 / 3.6.1 / 3.8).</summary>
    public sealed class DefinitionRef : RefTarget
    {
        public required Reference<IInterlisDefinition> Reference { get; init; }
    }

    /// <summary>An <c>ANYCLASS</c> / <c>ANYSTRUCTURE</c> placeholder standing in for any class/structure (RefHB 3.6.1-15/-17).</summary>
    public sealed class AnyRef : RefTarget
    {
        public required AnyKind Kind { get; init; }
    }

    /// <summary>Which <c>ANY…</c> keyword was used.</summary>
    public enum AnyKind
    {
        /// <summary><c>ANYCLASS</c> — any class or association (RefHB 3.6.1-15).</summary>
        Class,

        /// <summary><c>ANYSTRUCTURE</c> — any structure (RefHB 3.6.1-17).</summary>
        Structure,
    }
}
