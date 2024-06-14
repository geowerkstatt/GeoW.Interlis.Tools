namespace Geowerkstatt.Interlis.Tools.AST;

public class RoleType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }

    /// <summary>
    /// The accepted target classes with their respective restrictions.
    /// </summary>
    public List<RestrictedRef> Targets { get; } = new List<RestrictedRef>();
}
