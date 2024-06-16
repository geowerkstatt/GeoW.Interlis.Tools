namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class OidType : ITypeDef
{
    public static readonly ITypeDef? NoOid = null;

    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    /// <summary>
    /// The actual typedef, can be text or numeric.
    /// </summary>
    public ITypeDef? TypeDef { get; set; } = NoOid;
}
