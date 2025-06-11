using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class Subtraction: BinaryExpression
{
    public Subtraction() : base(new NumericType())
    {
    }
}
