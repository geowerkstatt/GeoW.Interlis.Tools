using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

public class FunctionDef : InterlisDefinition
{
    public required TypeDef ReturnType { get; init; }

    /// <summary>
    /// The function arguments (<c>argumentName : argumentType</c>), in order (RefHB 3.14).
    /// </summary>
    public List<FunctionArgument> Arguments { get; } = new List<FunctionArgument>();

    /// <summary>
    /// The explanation (<c>//...//</c>) following the return type, if any (RefHB 3.2.6/3.14).
    /// </summary>
    public string? Explanation { get; set; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitFunctionDef(this);
    }
}

/// <summary>
/// A single function argument (<c>argumentName : argumentType</c>, RefHB 3.14).
/// </summary>
public sealed class FunctionArgument
{
    public required string Name { get; init; }
    public required TypeDef Type { get; init; }
}
