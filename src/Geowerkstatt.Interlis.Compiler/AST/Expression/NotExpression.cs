using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class NotExpression : UnaryExpression
{
    public NotExpression() : base(new BooleanType())
    {
    }
}
