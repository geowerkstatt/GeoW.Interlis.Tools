namespace Geowerkstatt.Interlis.Tools.AST.Types;

public interface ITypeDef : IExtending<ITypeDef>
{
    Cardinality? Cardinality { get; set; }
}
