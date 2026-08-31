using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// A reference to a base unit within a unit's conversion expression (RefHB 3.9): the unit a derived or composed
/// unit is defined from. Unlike a <see cref="PathExpression"/> (object-path navigation), this is a direct typed
/// reference to a <see cref="UnitDef"/>, so it resolves and type-checks like any other unit reference while still
/// composing into the arithmetic conversion tree as a scalar factor.
/// </summary>
public sealed class UnitReferenceExpression : IExpression
{
    public required Reference<UnitDef> Unit { get; init; }

    public TypeDef ReturnType { get; } = new NumericType();
}
