namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// An OID value range (<c>OID ...</c>, RefHB 3.8.9-3), assignable to topics and classes (<c>OID AS</c>) or
/// usable as an ordinary attribute type.
/// </summary>
public class OidType : TypeDef
{
    /// <summary>
    /// The value range of the identifications: a concrete <see cref="ValueRange"/>, the open <see cref="AnyOid"/>
    /// or the unstable <see cref="NoOid"/> root; <see langword="null"/> only when the parser could not build it.
    /// </summary>
    public Target? Value { get; set; }

    /// <summary>
    /// The identification value range of an <see cref="OidType"/>; the states are mutually exclusive by
    /// construction.
    /// </summary>
    public abstract class Target
    {
    }

    /// <summary>
    /// <c>OID ANY</c>: identifications are expected, but their value range is still open (RefHB 3.8.9-3/-14).
    /// </summary>
    public sealed class AnyOid : Target
    {
    }

    /// <summary>
    /// The unstable-identifier state at the root of the predefined OID ladder (RefHB 3.8.9-6). No syntax
    /// produces it — Annex A declares the <c>NOOID</c> domain as <c>OID ANY</c> — but carrying the state as its
    /// own type lets the checks recognize the root (and aliases of it) without comparing domain identity.
    /// Carried only by the predefined <c>INTERLIS.NOOID</c>.
    /// </summary>
    public sealed class NoOid : Target
    {
    }

    /// <summary>
    /// A concrete identification value range: a text or numeric type (RefHB 3.8.9-3).
    /// </summary>
    public sealed class ValueRange : Target
    {
        public required TypeDef Type { get; init; }
    }
}
