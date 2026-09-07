using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public abstract class BinaryExpression(TypeDef returnType) : IExpression
{
    public TypeDef ReturnType { get; } = returnType;

    public required IExpression FirstOperand { get; init; }
    public required IExpression SecondOperand { get; init; }
}
