using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// An <c>ALL [ ( RestrictedClassOrAssRef ) ]</c> function argument (RefHB 3.13-32): all objects of the constraint's
/// context viewable, or of the given restriction. The restriction is carried by the <see cref="ObjectType.Targets"/>
/// of the <see cref="ReturnType"/>.
/// </summary>
public class AllExpression(RestrictedRef? restriction = null) : IExpression
{
    public TypeDef ReturnType { get; } = new ObjectType { Targets = restriction is null ? [] : [restriction] };
}
