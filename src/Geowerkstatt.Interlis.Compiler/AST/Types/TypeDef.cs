namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public abstract class TypeDef : IExtending<DomainDef>
{
    public Cardinality? Cardinality { get; set; }
    public Reference<DomainDef>? Extends { get; set; }

    public List<DomainConstraint> Constraints { get; } = new List<DomainConstraint>();
}
