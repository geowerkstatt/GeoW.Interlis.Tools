using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

public class DomainConstraint
{
    public required string Name { get; init; }

    public required IExpression Condition { get; set; }
}
