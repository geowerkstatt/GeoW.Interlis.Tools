using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

public sealed class AttributeDef : InterlisDefinition
{
    public override string FullyQualifiedName => Parent != null ? $"{Parent.FullyQualifiedName} -> {Name}" : Name;

    /// <summary>
    /// The attribute's type. Settable so the reference resolver can rewrite a provisional
    /// <see cref="Types.UnresolvedNamedType"/> into its resolved form (a <see cref="Types.TypeRef"/> domain alias or a
    /// <see cref="Types.ObjectType"/> substructure) once the target is known.
    /// </summary>
    public required TypeDef TypeDef { get; set; }

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    /// <summary>
    /// Whether the attribute is declared as a numeric subdivision of its precursor attribute
    /// (<c>[CONTINUOUS] SUBDIVISION</c>, RefHB 3.6.1-1/-3; e.g. minutes as a subdivision of hours). A single
    /// value instead of two flags, so the unwritable "CONTINUOUS without SUBDIVISION" is not representable.
    /// </summary>
    public SubdivisionKind Subdivision { get; set; }

    /// <summary>The subdivision forms of an attribute (see <see cref="Subdivision"/>).</summary>
    public enum SubdivisionKind
    {
        /// <summary>Not a subdivision.</summary>
        None,

        /// <summary><c>SUBDIVISION</c>: the value subdivides one unit of the precursor attribute.</summary>
        Subdivision,

        /// <summary><c>CONTINUOUS SUBDIVISION</c>: the subdivision is additionally seamless — the value range covers the precursor's unit completely and evenly (RefHB 3.6.1-1).</summary>
        ContinuousSubdivision,
    }

    /// <summary>
    /// The factor expression(s) assigned with <c>:=</c> (a default/derived value). Multiple factors can be
    /// given within views and view extensions; the last defined one applies (RefHB 3.6.1-4/6).
    /// </summary>
    public List<IExpression> Values { get; } = new List<IExpression>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitAttributeDef(this);
    }
}
