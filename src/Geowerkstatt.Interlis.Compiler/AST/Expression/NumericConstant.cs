using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class NumericConstant : ConstantExpression
{
    public required double Value { get; init; }

    public NumericConstant() : base(new NumericType())
    {
    }
}
