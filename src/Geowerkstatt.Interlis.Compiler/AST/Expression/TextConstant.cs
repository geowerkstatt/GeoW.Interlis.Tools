using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class TextConstant : ConstantExpression
{
    public required string Value { get; init; }

    public TextConstant() : base(new TextType())
    {
    }
}
