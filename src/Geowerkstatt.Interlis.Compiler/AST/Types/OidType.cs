namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class OidType : TypeDef
{
    public static readonly TypeDef? NoOid = null;

    /// <summary>
    /// The actual typedef, can be text or numeric.
    /// </summary>
    public TypeDef? TypeDef { get; set; } = NoOid;
}
