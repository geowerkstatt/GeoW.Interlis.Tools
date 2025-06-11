using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class AndExpression : BinaryExpression
{
    public AndExpression() : base(new BooleanType())
    {
    }
}
