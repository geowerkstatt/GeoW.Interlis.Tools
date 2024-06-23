using Geowerkstatt.Interlis.Tools.AST.Expression;

namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class DomainConstraint
{
    public required string Name { get; init; }

    public required IExpression Condition { get; set; }
}
