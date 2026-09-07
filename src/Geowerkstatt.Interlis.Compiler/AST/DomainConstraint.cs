using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A named value restriction of a domain definition (<c>CONSTRAINTS Name ':' Logical-Expression</c>,
/// RefHB 3.8-8/-10). Deliberately not part of the <see cref="ConstraintDef"/> hierarchy: it restricts the VALUES
/// of a type rather than the objects of a container, its name is grammar-mandatory (never synthesized) and it
/// lives in no namespace, so the definition machinery (parent, name index, translation link) does not apply.
/// </summary>
public class DomainConstraint : IVisitable, ISourceRange
{
    public required string Name { get; init; }

    public required IExpression Condition { get; set; }

    public RangePosition? SourceRange { get; init; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitDomainConstraint(this);
    }
}
