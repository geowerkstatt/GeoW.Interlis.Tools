using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class GreaterThanExpression : BinaryExpression
{
    public GreaterThanExpression() : base(new BooleanType())
    {
    }
}
