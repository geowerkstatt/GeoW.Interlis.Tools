using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public abstract class ConstantExpression(TypeDef returnType) : IExpression
{
    public virtual TypeDef ReturnType { get; } = returnType;
}
