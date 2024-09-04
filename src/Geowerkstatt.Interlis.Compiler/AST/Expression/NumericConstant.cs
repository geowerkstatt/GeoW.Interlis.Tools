using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class NumericConstant : ConstantExpression
{
    public required double Value { get; init; }

    public NumericConstant() : base(new NumericType())
    {
    }
}
