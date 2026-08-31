using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A type definition that has a name and can therefore be referenced.
/// </summary>
public class DomainDef : InterlisDefinition
{
    public required TypeDef TypeDef { get; init; }


    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitDomainDef(this);
    }
}
