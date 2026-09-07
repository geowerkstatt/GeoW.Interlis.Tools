using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A value domain that holds an attribute path (keyword <c>ATTRIBUTE</c>, RefHB 3.8.11). The path may be
/// required to belong to a class given by another definition (<c>OF</c> object/attribute path) or, inside a
/// function, to another argument (<c>OF @ argument</c>). The allowed attribute types can be narrowed with
/// <c>RESTRICTION</c>.
/// </summary>
public class AttributePathType : TypeDef
{
    /// <summary>
    /// The object/attribute path given after <c>OF</c>, if any.
    /// </summary>
    public PathExpression? Of { get; set; }

    /// <summary>
    /// The function argument name given after <c>OF @</c>, if any.
    /// </summary>
    public string? ArgumentName { get; set; }

    /// <summary>
    /// The allowed attribute types listed in <c>RESTRICTION (...)</c>, if any.
    /// </summary>
    public List<TypeDef> Restrictions { get; } = new List<TypeDef>();
}
