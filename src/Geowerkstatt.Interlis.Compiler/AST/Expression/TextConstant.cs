using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class TextConstant : ConstantExpression
{
    public required string Value { get; init; }

    public TextConstant() : base(new TextType())
    {
    }
}
