using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class FunctionCall : IExpression
{
    /// <inheritdoc />
    public RangePosition? SourceRange { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// Is resolved from the <see cref="FunctionDef"/>. Falls back to <see cref="UndefinedType.Instance"/>
    /// when the function reference is unresolved, so consumers never observe a null return type.
    /// </remarks>
    public TypeDef ReturnType => FunctionDef.Target?.ReturnType ?? UndefinedType.Instance;

    public required Reference<FunctionDef> FunctionDef { get; init; }

    public IList<IExpression> Arguments { get; } = new List<IExpression>();
}
