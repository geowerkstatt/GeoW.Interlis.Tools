using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public abstract class ConstantExpression(TypeDef returnType) : IExpression
{
    /// <inheritdoc />
    public RangePosition? SourceRange { get; init; }

    public virtual TypeDef ReturnType { get; } = returnType;
}
