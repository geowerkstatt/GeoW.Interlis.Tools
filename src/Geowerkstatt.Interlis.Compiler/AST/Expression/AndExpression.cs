using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class AndExpression : BinaryExpression
{
    public AndExpression() : base(new BooleanType())
    {
    }
}
