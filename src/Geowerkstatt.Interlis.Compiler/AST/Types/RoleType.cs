namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class RoleType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    /// <summary>
    /// The accepted target classes with their respective restrictions.
    /// </summary>
    public List<RestrictedRef> Targets { get; } = new List<RestrictedRef>();
}
