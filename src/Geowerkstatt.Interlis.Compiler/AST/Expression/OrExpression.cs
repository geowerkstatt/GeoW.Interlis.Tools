using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class OrExpression : BinaryExpression
{
    public OrExpression() : base(new BooleanType())
    {
    }
}
