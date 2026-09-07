namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An AST element that declares consistency constraints (RefHB 3.12), e.g. a class, association, view or an
/// external <c>CONSTRAINTS OF</c> block. The constraints are held in a list rather than in
/// <see cref="IContainer{T}.Content"/> because a constraint name is for diagnostic messages only, is not part of
/// the namespace, and may be duplicated.
/// </summary>
public interface IConstraintContainer
{
    /// <summary>
    /// The consistency constraints declared on this element, in source order.
    /// </summary>
    List<ConstraintDef> Constraints { get; }
}
