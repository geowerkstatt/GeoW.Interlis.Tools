
namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public abstract class TypeDef : IExtending<DomainDef>, ISourceRange
{
    public Cardinality? Cardinality { get; set; }
    public Reference<DomainDef>? Extends { get; set; }

    public List<DomainConstraint> Constraints { get; } = new List<DomainConstraint>();

    public RangePosition? SourceRange { get; init; }
}
