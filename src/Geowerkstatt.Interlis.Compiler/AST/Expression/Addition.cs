using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class Addition : BinaryExpression
{
    public Addition() : base(new NumericType())
    {
    }
}
