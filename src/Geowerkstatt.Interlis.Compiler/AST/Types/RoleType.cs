namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// The type of an association role (RefHB 3.7). A role follows the rules of reference attributes
/// (RefHB 3.7.1-1): the link instance holds the reference to the target object as its value. Unlike on a value
/// type, the inherited <see cref="TypeDef.Cardinality"/> is a POPULATION constraint — how many links a source
/// object may participate in (RefHB 3.7.3) — not the size of an owned value.
/// </summary>
public class RoleType : TypeDef
{
    /// <summary>
    /// The accepted target classes with their respective restrictions.
    /// </summary>
    public List<RestrictedRef> Targets { get; } = new List<RestrictedRef>();

    /// <summary>
    /// The strength of the relationship this role establishes (RefHB 3.7.1: <c>--</c> / <c>-&lt;&gt;</c> /
    /// <c>-&lt;#&gt;</c>).
    /// </summary>
    public RelationshipType Relationship { get; set; } = RelationshipType.Association;

    public enum RelationshipType
    {
        /// <summary>
        /// Loose connection
        /// </summary>
        Association = Interlis24Parser.ASSOCIATION_SYMBOL,

        /// <summary>
        /// Feeble relationship between the entirety and its parts.
        /// </summary>
        Aggregation = Interlis24Parser.AGGREGATION_SYMBOL,

        /// <summary>
        /// Strong relationship between the entirety and its parts.
        /// </summary>
        Composition = Interlis24Parser.COMPOSITION_SYMBOL,
    }
}
