namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class ReferenceType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    public required RestrictedRef Target { get; init; }
}
