namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class ReferenceType : TypeDef
{
    public required RestrictedRef Target { get; init; }
    public HashSet<Property> Properties { get; } = new HashSet<Property>();
}
