using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// An object or attribute path (RefHB 3.13, <c>ObjectOrAttributePath = PathEl { '-&gt;' PathEl }</c>): a chain of
/// <see cref="IPathElement"/> steps navigating from a context object to the object or attribute value it denotes.
/// </summary>
public class PathExpression : ConstantExpression
{
    public IList<IPathElement> Path { get; } = new List<IPathElement>();

    /// <summary>
    /// The definition the path resolves to: the definition reached by following the path to its last element — an
    /// attribute or role (<see cref="AttributeDef"/>), a class/structure/association or view, or a view base
    /// (<see cref="BaseView"/>). Used for language-server go-to-definition.
    /// <para>
    /// <see langword="null"/> while the path is unresolved — a rule-level parse without reference resolution, an
    /// element that could not be resolved (unknown member, an unqualified keyword such as <c>PARENT</c>), or a path
    /// whose head has no resolvable context. Set by <see cref="CreateAST.Interlis24AstPathResolverVisitor"/> after
    /// reference resolution.
    /// </para>
    /// </summary>
    public IInterlisDefinition? Target { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The type reached at the tip of the path, derived from <see cref="Target"/> and the last <see cref="Path"/>
    /// element (so it stays consistent with <see cref="Target"/> and needs no separate storage): the declared type of
    /// a scalar attribute; an <see cref="ObjectType"/> — carrying the viewable(s) the object belongs to in
    /// <see cref="ObjectType.Targets"/> — for a step that denotes an object (a role, reference attribute,
    /// substructure, class, base or <c>THIS</c>); and — <b>more specific</b> than <see cref="Target"/> declares — the
    /// single element an indexed last step selects (a <c>COORD</c> axis <see cref="NumericType"/>, or one element of an
    /// ordered <c>LIST OF</c> substructure). <see cref="UndefinedType"/> while the path is unresolved.
    /// </remarks>
    public override TypeDef ReturnType => Target switch
    {
        AttributeDef attribute => TipTypeOf(attribute),
        BaseView { Viewable: { } viewable } => new ObjectType { Targets = [new RestrictedRef { Value = viewable }] },
        BaseView => new ObjectType(), // a base whose viewable is missing after a parse error
        { } definition => new ObjectType { Targets = [new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = definition } }] }, // a class / structure / view was reached: the tip denotes an object of it
        _ when Path.Count > 0 && Path[^1] is KeyWordPathElement => new ObjectType(), // THIS / PARENT / …AREA: an object whose definition is unknown
        _ => UndefinedType.Instance,
    };

    public PathExpression() : base(UndefinedType.Instance)
    {
    }

    /// <summary>
    /// The tip type reached at an attribute <paramref name="attribute"/>: the single element/axis selected by an
    /// indexed last step, an <see cref="ObjectType"/> for an object-valued attribute, otherwise the attribute's own type.
    /// </summary>
    private TypeDef TipTypeOf(AttributeDef attribute)
    {
        // A named-domain alias is transparent: the tip has the domain's underlying type. An alias whose domain is
        // unresolved stays a TypeRef, which consumers treat as unknown.
        var type = attribute.TypeDef.Underlying();

        // An indexed last step narrows the attribute to a single element/axis (more specific than its declared type).
        if (Path.Count > 0 && Path[^1] is AttributePathElement { Index: var index })
        {
            return type switch
            {
                CoordType { Axis: var axis } => AxisIndex(index, axis.Count) is { } i ? axis[i] : UndefinedType.Instance,
                ObjectType substructure => new ObjectType { Targets = substructure.Targets }, // one element of an ordered LIST OF substructure
                _ => UndefinedType.Instance,
            };
        }

        return type switch
        {
            RoleType role => new ObjectType { Targets = role.Targets },
            ReferenceType reference => new ObjectType { Targets = [reference.Target] },
            // A contained substructure denotes its instance(s); rebuilt fresh so the owned-collection cardinality
            // does not ride along on the tip.
            ObjectType substructure => new ObjectType { Targets = substructure.Targets },
            ClassType classType => new ObjectType { Targets = classType.Restrictions.Select(r => new RestrictedRef { Value = r }).ToList() },
            _ => type,
        };
    }

    /// <summary>The valid 0-based axis a coordinate index selects, or <see langword="null"/> if it is out of range (so the caller can index safely).</summary>
    private static int? AxisIndex(AttributePathElement.PathIndex index, int axisCount) => index switch
    {
        AttributePathElement.KeywordIndex { Kind: AttributePathElement.IndexKeyword.First } when axisCount > 0 => 0,
        AttributePathElement.KeywordIndex { Kind: AttributePathElement.IndexKeyword.Last } when axisCount > 0 => axisCount - 1,
        AttributePathElement.NumberIndex { Value: var value } when value >= 1 && value <= axisCount => value - 1,
        _ => null,
    };
}
