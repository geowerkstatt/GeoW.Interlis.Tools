using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class UndefinedConstant : ConstantExpression
{
    public UndefinedConstant() : base(UndefinedType.Instance)
    {
    }
}
