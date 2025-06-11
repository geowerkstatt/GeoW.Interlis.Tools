using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public abstract class UnaryExpression(TypeDef returnType) : IExpression
{
    public TypeDef ReturnType { get; init; } = returnType;

    public required IExpression Operand { get; init; }
}
