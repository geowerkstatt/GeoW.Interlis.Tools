namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class EnumerationAllOfType : TypeDef
{
    public Reference<EnumerationType>? TargetEnumeration { get; set; }
}
