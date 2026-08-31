using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// A run-time parameter used as an expression factor (<c>PARAMETER [Model.]Name</c>, RefHB 3.13-25): the value the
/// runtime system supplies for the referenced parameter (RefHB 3.11), typically compared against in graphic
/// definitions (RefHB 3.13-53, 3.16) — e.g. the current map scale.
/// </summary>
public class ParameterRefExpression : IExpression
{
    /// <summary>The referenced run-time parameter.</summary>
    public required Reference<ParameterDef> Parameter { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// The declared type of the referenced parameter (derived so it stays consistent once the reference is
    /// resolved); a named-domain alias is transparent, like at a path tip. <see cref="UndefinedType"/> while the
    /// reference is unresolved.
    /// </remarks>
    public TypeDef ReturnType => Parameter.Target?.TypeDef?.Underlying() ?? UndefinedType.Instance;
}
