using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class EqualExpression : BinaryExpression
{
    public EqualExpression() : base(new BooleanType())
    {
    }
}
