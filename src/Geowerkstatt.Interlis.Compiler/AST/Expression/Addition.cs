using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class Addition : BinaryExpression
{
    public Addition() : base(new NumericType())
    {
    }
}
