using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

public class UnitDef : InterlisDefinition, IExtending<UnitDef>
{
    /// <summary>
    /// The term used to define the unit.
    /// A unit can not be referenced by its term.
    /// </summary>
    public required string Term { get; init; }

    public Reference<UnitDef>? Extends { get; set; }


    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public IExpression? Expression { get; set; }

    /// <summary>
    /// The explanation (<c>//...//</c>) of a <c>FUNCTION</c>-defined derived unit, if any (RefHB 3.2.6/3.9.2).
    /// For such units the conversion is described informally by this explanation rather than by a formula.
    /// </summary>
    public string? Explanation { get; set; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitUnitDef(this);
    }
}
