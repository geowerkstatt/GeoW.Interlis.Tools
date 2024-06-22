namespace Geowerkstatt.Interlis.Tools.AST.Types;

public abstract class TypeDef : IExtending<TypeDef>
{
    public Cardinality? Cardinality { get; set; }
    public TypeDef? Extends { get; set; }
}
