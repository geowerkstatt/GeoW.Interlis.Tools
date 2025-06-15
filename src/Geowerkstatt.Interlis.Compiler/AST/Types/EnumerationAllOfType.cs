namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class EnumerationAllOfType : TypeDef
{
    public Reference<DomainDef>? TargetEnumeration { get; set; }
}
