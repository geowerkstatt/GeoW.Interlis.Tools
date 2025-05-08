namespace Geowerkstatt.Interlis.Tools.AST;

public class UnitDef : IInterlisDefinition, IDocumentation, IExtending<UnitDef>
{
    /// <summary>
    /// The term used to define the unit.
    /// A unit can not be referenced by its term.
    /// </summary>
    public required string Term { get; init; }

    /// <summary>
    /// The short name of this unit as defined in square brackets in the INTERLIS syntax.
    /// This name is used to reference the unit.
    /// </summary>
    public required string Name { get; init; }
    public IInterlisDefinitionContainer? Parent { get; set; }

    public Reference<UnitDef>? Extends { get; set; }

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitUnitDef(this);
    }
}
