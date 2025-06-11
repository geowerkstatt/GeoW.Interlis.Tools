namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class RoleType : TypeDef
{
    /// <summary>
    /// The accepted target classes with their respective restrictions.
    /// </summary>
    public List<RestrictedRef> Targets { get; } = new List<RestrictedRef>();
}
