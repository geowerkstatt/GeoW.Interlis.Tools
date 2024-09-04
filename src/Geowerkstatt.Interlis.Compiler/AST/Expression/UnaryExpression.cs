using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public abstract class UnaryExpression(TypeDef returnType) : IExpression
{
    public TypeDef ReturnType { get; init; } = returnType;

    public required IExpression Operand { get; init; }
}
