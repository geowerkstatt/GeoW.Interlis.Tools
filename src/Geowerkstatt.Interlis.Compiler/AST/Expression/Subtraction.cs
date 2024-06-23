using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class Subtraction: BinaryExpression
{
    public Subtraction() : base(new NumericType())
    {
    }
}
