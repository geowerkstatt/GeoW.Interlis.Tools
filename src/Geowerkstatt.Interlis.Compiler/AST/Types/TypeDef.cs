namespace Geowerkstatt.Interlis.Tools.AST.Types;

public abstract class TypeDef : IExtending<TypeDef>
{
    public Cardinality? Cardinality { get; set; }
    public Reference<TypeDef>? Extends { get; set; }

    public List<DomainConstraint> Constraints { get; } = new List<DomainConstraint>();
}
