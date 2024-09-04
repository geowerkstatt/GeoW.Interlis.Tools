using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class DefinedExpression : UnaryExpression
{
    public DefinedExpression() : base(new BooleanType())
    {
    }
}
