using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// An attribute-path constant (RefHB 3.13 <c>AttributePathConst = '&gt;&gt;' [ ViewableRef '-&gt;' ] Attribute-Name</c>):
/// a value denoting an attribute of a viewable, e.g. <c>&gt;&gt;Model.Topic.MyClass-&gt;Attr</c> or, for the current
/// object's class, <c>&gt;&gt;Attr</c>. The optional viewable qualification and the attribute name together form a
/// single reference to the attribute; the constant's <see cref="ReturnType"/> is that attribute's type.
/// </summary>
public sealed class AttributePathConstant : ConstantExpression
{
    /// <summary>The referenced attribute.</summary>
    public required Reference<AttributeDef> Attribute { get; init; }

    /// <inheritdoc />
    /// <remarks>The type of the referenced attribute; <see cref="UndefinedType.Instance"/> while it is unresolved.</remarks>
    public override TypeDef ReturnType => Attribute.Target?.TypeDef ?? UndefinedType.Instance;

    public AttributePathConstant() : base(UndefinedType.Instance)
    {
    }
}
