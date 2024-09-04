using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class Multiplication : BinaryExpression
{
    public Multiplication() : base(new NumericType())
    {
    }
}
