namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class ReferenceType : TypeDef
{
    public required RestrictedRef Target { get; init; }
    public HashSet<Property> Properties { get; } = new HashSet<Property>();
}
