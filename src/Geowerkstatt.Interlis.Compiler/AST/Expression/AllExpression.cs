using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class AllExpression : IExpression
{
    public TypeDef ReturnType { get; init; } = new ObjectType();

    public RestrictedRef? Restriction { get; init; }
}
