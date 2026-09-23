using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A view definition (<c>VIEW</c>, RefHB 3.15). A view is a viewable derived from other viewables via a
/// formation (projection/join/union/aggregation/inspection) or by extending another view, optionally
/// restricted by selections and carrying its own (derived) attributes and constraints.
/// </summary>
public sealed class ViewDef : InterlisDefinition, IInterlisDefinitionContainer, IConstraintContainer, IExtending<ViewDef>
{
    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();
    public ICollection<IReference> ContainerReferences { get; } = new List<IReference>();

    /// <summary>
    /// The view this view extends (<c>EXTENDS</c>), if any (mutually exclusive with <see cref="Formation"/>).
    /// </summary>
    public Reference<ViewDef>? Extends { get; set; }

    /// <summary>
    /// How the view is formed from base viewables (RefHB 3.15); <see langword="null"/> when the view extends
    /// another view instead.
    /// </summary>
    public ViewFormation? Formation { get; set; }

    /// <summary>
    /// <c>BASE base EXTENDED BY ...</c> declarations (RefHB 3.15).
    /// </summary>
    public List<BaseExtension> BaseExtensions { get; } = new List<BaseExtension>();

    /// <summary>
    /// The selection conditions (<c>WHERE</c>) restricting the objects the view contains.
    /// </summary>
    public List<IExpression> Selections { get; } = new List<IExpression>();

    /// <summary>
    /// The bases whose attributes are all taken over (<c>ALL OF Base</c>), as registered references to the
    /// view's <see cref="BaseView"/> entries — an unknown base name stays unresolved and is reported.
    /// </summary>
    public List<Reference<BaseView>> AllOfBases { get; } = new List<Reference<BaseView>>();

    /// <summary>
    /// The consistency constraints declared on the view (RefHB 3.12/3.15).
    /// </summary>
    public List<ConstraintDef> Constraints { get; } = new List<ConstraintDef>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitViewDef(this);
    }
}

/// <summary>
/// A <c>BASE base EXTENDED BY ViewableRef {, ViewableRef}</c> declaration of a view (RefHB 3.15).
/// </summary>
public sealed class BaseExtension
{
    public required string Base { get; init; }
    public List<BaseView> ExtendedBy { get; } = new List<BaseView>();
}

/// <summary>
/// Base class for the way a <see cref="ViewDef"/> is formed from other viewables (RefHB 3.15).
/// </summary>
public abstract class ViewFormation
{
}

/// <summary><c>PROJECTION OF</c> a single viewable.</summary>
public sealed class ProjectionView : ViewFormation
{
    public required BaseView Source { get; init; }
}

/// <summary><c>JOIN OF</c> several viewables, each optionally marked <c>(OR NULL)</c>.</summary>
public sealed class JoinView : ViewFormation
{
    public List<JoinViewSource> Sources { get; } = new List<JoinViewSource>();
}

/// <summary>One viewable of a <see cref="JoinView"/>, with its optional <c>(OR NULL)</c> flag.</summary>
public sealed class JoinViewSource
{
    public required BaseView Viewable { get; init; }
    public bool OrNull { get; init; }
}

/// <summary><c>UNION OF</c> several viewables.</summary>
public sealed class UnionView : ViewFormation
{
    public List<BaseView> Sources { get; } = new List<BaseView>();
}

/// <summary><c>AGGREGATION OF</c> a viewable, grouping <c>ALL</c> or by a set of unique attribute paths.</summary>
public sealed class AggregationView : ViewFormation
{
    public required BaseView Source { get; init; }

    /// <summary>Whether <c>ALL</c> objects are aggregated (otherwise grouped by <see cref="UniqueBy"/>).</summary>
    public bool All { get; set; }

    /// <summary>The attribute paths to group by (<c>EQUAL ( ... )</c>).</summary>
    public List<PathExpression> UniqueBy { get; } = new List<PathExpression>();
}

/// <summary><c>[AREA] INSPECTION OF</c> a viewable, following a structure attribute path.</summary>
public sealed class InspectionView : ViewFormation
{
    public bool IsArea { get; set; }
    public required BaseView Source { get; init; }

    /// <summary>
    /// The attribute path inspected (<c>-&gt; attr -&gt; attr ...</c>, RefHB 3.15-15/-33): every step but the last
    /// is a substructure attribute of the previous step's structure (the first step of the source viewable); the
    /// last step may also be a line attribute. An <see cref="ReferenceResolution.ObjectPath"/> the path resolver
    /// walks while checking it; its target is the attribute the last step reaches.
    /// </summary>
    public required Reference<AttributeDef> Path { get; init; }
}
