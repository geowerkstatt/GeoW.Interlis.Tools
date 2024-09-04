using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class EqualExpression : BinaryExpression
{
    public EqualExpression() : base(new BooleanType())
    {
    }
}
