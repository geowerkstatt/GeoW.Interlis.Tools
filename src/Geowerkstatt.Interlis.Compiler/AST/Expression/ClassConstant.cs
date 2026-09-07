using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// A class constant (RefHB 3.13 <c>ClassConst = '&gt;' ViewableRef</c>): a value denoting a viewable — a class,
/// structure, association or view — e.g. <c>&gt;Model.Topic.MyClass</c>. Unlike an object/attribute path it is a
/// constant that names a definition, so it is modelled as a <see cref="ConstantExpression"/> holding a
/// <see cref="Reference{T}"/> rather than as a <see cref="PathExpression"/>.
/// </summary>
public sealed class ClassConstant : ConstantExpression
{
    /// <summary>The referenced viewable (class, structure, association or view).</summary>
    public required Reference<IInterlisDefinition> Viewable { get; init; }

    /// <inheritdoc />
    /// <remarks>An object of the referenced <see cref="Viewable"/>; derived so it stays consistent once the reference is resolved.</remarks>
    public override TypeDef ReturnType => new ObjectType { Targets = [new RestrictedRef { Value = Viewable }] };

    public ClassConstant() : base(UndefinedType.Instance)
    {
    }
}
