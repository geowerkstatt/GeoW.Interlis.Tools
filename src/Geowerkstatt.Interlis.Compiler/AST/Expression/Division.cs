using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class Division : BinaryExpression
{
    public Division() : base(new NumericType())
    {
    }
}
