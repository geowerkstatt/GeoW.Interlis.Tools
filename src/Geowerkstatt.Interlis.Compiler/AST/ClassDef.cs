using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

public sealed class ClassDef : InterlisDefinition, IExtending<ClassDef>, IInterlisDefinitionContainer, IConstraintContainer, IIdentifiable
{
    public Reference<ClassDef>? Extends { get; set; }

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    /// <summary>
    /// The consistency constraints declared in this class/structure (RefHB 3.12). Kept in a list rather than in
    /// <see cref="Content"/> because constraint names may be duplicated (they are for messages only, not the namespace).
    /// </summary>
    public List<ConstraintDef> Constraints { get; } = new List<ConstraintDef>();

    /// <inheritdoc />
    public Dictionary<string, AssociationDef> AssociationAccess { get; } = new Dictionary<string, AssociationDef>();

    public bool IsStructure { get; init; }

    public Reference<DomainDef>? OidType { get; set; }

    public ICollection<IReference> ContainerReferences { get; } = new List<IReference>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitClassDef(this);
    }
}
