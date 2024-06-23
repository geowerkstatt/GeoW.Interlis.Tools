using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public class UndefinedConstant : ConstantExpression
{
    public UndefinedConstant() : base(UndefinedType.Instance)
    {
    }
}
