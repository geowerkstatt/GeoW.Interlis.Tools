using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class Multiplication : BinaryExpression
{
    public Multiplication() : base(new NumericType())
    {
    }
}
