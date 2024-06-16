namespace Geowerkstatt.Interlis.Tools.AST.Types;

/// <summary>
/// Represents 'OID ANY'
/// </summary>
public class OidAnyType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }
}
