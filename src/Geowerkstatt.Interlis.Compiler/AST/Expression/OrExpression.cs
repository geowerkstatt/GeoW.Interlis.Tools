using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class OrExpression : BinaryExpression
{
    public OrExpression() : base(new BooleanType())
    {
    }
}
