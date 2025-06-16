using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class FunctionCall : IExpression
{
    /// <inheritdoc />
    /// <remarks>Is resolved from the <see cref="FunctionDef"/>.</remarks>
    public TypeDef ReturnType => FunctionDef.Target?.ReturnType!;

    public required Reference<FunctionDef> FunctionDef { get; init; }

    public IList<IExpression> Arguments { get; } = new List<IExpression>();
}
