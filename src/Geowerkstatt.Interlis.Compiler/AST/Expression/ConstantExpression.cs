using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public abstract class ConstantExpression(TypeDef returnType) : IExpression
{
    public TypeDef ReturnType { get; init; } = returnType;
}
