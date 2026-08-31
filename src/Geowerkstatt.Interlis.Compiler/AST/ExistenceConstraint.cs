using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An <c>EXISTENCE CONSTRAINT</c>: the value of an attribute path must also exist in one of the listed
/// viewable/attribute-path combinations (RefHB 3.12).
/// </summary>
public sealed class ExistenceConstraint : ConstraintDef
{
    public required PathExpression AttributePath { get; init; }

    /// <summary>
    /// The <c>REQUIRED IN ViewableRef ':' AttributePath</c> alternatives.
    /// </summary>
    public List<ExistenceRequirement> RequiredIn { get; } = new List<ExistenceRequirement>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitExistenceConstraint(this);
    }
}

/// <summary>
/// One <c>ViewableRef ':' AttributePath</c> alternative of an <see cref="ExistenceConstraint"/>.
/// </summary>
public sealed class ExistenceRequirement
{
    public Reference<IInterlisDefinition>? Viewable { get; set; }
    public PathExpression? AttributePath { get; set; }
}
