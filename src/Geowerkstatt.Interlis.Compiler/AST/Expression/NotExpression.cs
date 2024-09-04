using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class NotExpression : UnaryExpression
{
    public NotExpression() : base(new BooleanType())
    {
    }
}
