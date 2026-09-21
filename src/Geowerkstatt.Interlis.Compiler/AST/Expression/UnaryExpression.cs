using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public abstract class UnaryExpression(TypeDef returnType) : IExpression
{
    /// <inheritdoc />
    public RangePosition? SourceRange { get; init; }

    public TypeDef ReturnType { get; } = returnType;

    public required IExpression Operand { get; init; }
}
