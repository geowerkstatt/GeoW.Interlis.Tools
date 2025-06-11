using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class Division : BinaryExpression
{
    public Division() : base(new NumericType())
    {
    }
}
