using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public abstract class BinaryExpression(TypeDef returnType) : IExpression
{
    public TypeDef ReturnType { get; init; } = returnType;

    public required IExpression FirstOperand { get; init; }
    public required IExpression SecondOperand { get; init; }
}
