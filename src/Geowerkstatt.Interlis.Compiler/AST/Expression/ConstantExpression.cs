using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public abstract class ConstantExpression(TypeDef returnType) : IExpression
{
    public TypeDef ReturnType { get; init; } = returnType;
}
