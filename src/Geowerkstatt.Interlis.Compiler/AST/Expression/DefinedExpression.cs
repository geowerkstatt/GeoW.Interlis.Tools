using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class DefinedExpression : UnaryExpression
{
    public DefinedExpression() : base(new BooleanType())
    {
    }
}
