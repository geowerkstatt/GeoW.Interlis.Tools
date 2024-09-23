namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class EnumerationAllOfType : TypeDef
{
    public Reference<EnumerationType>? TargetEnumeration { get; set; }
}
