using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

public class AssociationDef : InterlisDefinition, IInterlisDefinitionContainer, IConstraintContainer, IExtending<AssociationDef>, IIdentifiable
{
    public Reference<AssociationDef>? Extends { get; set; }

    /// <summary>
    /// The viewable (view/class) this association is <c>DERIVED FROM</c>, if any (RefHB 3.7.1, derived associations),
    /// as a base view carrying the optional base alias (<c>DERIVED FROM Base ~ ViewableRef</c>). It is the same
    /// instance registered in <see cref="Content"/> under its base name (a Bestandteilname, RefHB 3.5.4), so the
    /// role derivations can resolve their path head against it.
    /// </summary>
    public BaseView? DerivedFrom { get; set; }

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    /// <summary>
    /// The consistency constraints declared in this association (RefHB 3.12). Kept in a list rather than in
    /// <see cref="Content"/> because constraint names may be duplicated (they are for messages only, not the namespace).
    /// </summary>
    public List<ConstraintDef> Constraints { get; } = new List<ConstraintDef>();

    /// <inheritdoc />
    public Dictionary<string, AssociationDef> AssociationAccess { get; } = new Dictionary<string, AssociationDef>();

    public required Cardinality Cardinality { get; init; }

    public Reference<DomainDef>? OidType { get; set; }

    public ICollection<IReference> ContainerReferences { get; } = new List<IReference>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitAssociationDef(this);
    }
}
