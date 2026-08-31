namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A value domain that holds a reference to a class or structure (keywords <c>CLASS</c> / <c>STRUCTURE</c>,
/// RefHB 3.8.11). <c>STRUCTURE</c> allows any structure or class; <c>CLASS</c> allows any class. The allowed
/// targets can be narrowed with <c>RESTRICTION</c>.
/// </summary>
public class ClassType : TypeDef
{
    /// <summary>
    /// Whether the keyword was <c>STRUCTURE</c> (<see langword="true"/>) or <c>CLASS</c> (<see langword="false"/>).
    /// </summary>
    public bool IsStructure { get; set; }

    /// <summary>
    /// The allowed class/structure targets listed in <c>RESTRICTION (...)</c>, if any.
    /// </summary>
    public List<Reference<IInterlisDefinition>> Restrictions { get; } = new List<Reference<IInterlisDefinition>>();
}
