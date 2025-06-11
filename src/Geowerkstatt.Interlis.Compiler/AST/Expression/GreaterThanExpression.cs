using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class GreaterThanExpression : BinaryExpression
{
    public GreaterThanExpression() : base(new BooleanType())
    {
    }
}
