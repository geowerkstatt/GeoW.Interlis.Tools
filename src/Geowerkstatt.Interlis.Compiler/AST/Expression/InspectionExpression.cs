using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

/// <summary>
/// An inspection used as an expression factor (RefHB 3.13-25/-48): the set of structure elements an inspection
/// (RefHB 3.15) yields, written inline (<c>[AREA] INSPECTION OF Viewable -&gt; attr ...</c>) or referencing a named
/// inspection view (<c>INSPECTION ViewableRef</c>). Typically passed to a function whose parameter is
/// <c>OBJECTS OF</c> a structure (RefHB 3.14).
/// </summary>
public class InspectionExpression : IExpression
{
    /// <inheritdoc />
    public RangePosition? SourceRange { get; init; }

    /// <summary>The inspection evaluated: written inline or referencing a named inspection view.</summary>
    public required InspectionSource Source { get; init; }

    /// <summary>
    /// The optional <c>OF</c> restriction path (RefHB 3.13-48): only the structure elements belonging to the object
    /// the path denotes are part of the inspected set. Rooted at the context viewable like any other path in the
    /// enclosing expression; <see langword="null"/> when no restriction is written.
    /// </summary>
    public PathExpression? Of { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// The denoted set of structure elements, as an <see cref="ObjectType"/>. For a named inspection view the
    /// elements are objects of that view (derived from the reference, so it stays consistent once resolved); for an
    /// inline inspection they are elements of the substructure the resolved attribute path reaches (derived from
    /// <see cref="InspectionView.Path"/>'s tip target). Before resolution — or when the tip is a line attribute,
    /// whose elements are the predefined boundary/edge structures the inspection computes rather than an authored
    /// definition — the targets stay empty: "unknown" to consumers.
    /// </remarks>
    public TypeDef ReturnType => Source switch
    {
        ViewRef viewRef => new ObjectType { Targets = [new RestrictedRef { Value = viewRef.View }] },
        InlineInspection { Inspection.Path.Target.TypeDef: ObjectType substructure } => new ObjectType { Targets = substructure.Targets },
        _ => new ObjectType(),
    };

    /// <summary>
    /// The source of an <see cref="InspectionExpression"/>: an inline inspection formation
    /// (<see cref="InlineInspection"/>) or a reference to a named inspection view (<see cref="ViewRef"/>).
    /// The two are mutually exclusive by construction.
    /// </summary>
    public abstract class InspectionSource
    {
        /// <summary>An inline inspection is written directly; e.g. <c>Source = inspectionView</c> stands for <c>new InlineInspection { Inspection = inspectionView }</c>.</summary>
        public static implicit operator InspectionSource(InspectionView inspection) => new InlineInspection { Inspection = inspection };

        /// <summary>A view reference is written directly; e.g. <c>Source = someReference</c> stands for <c>new ViewRef { View = someReference }</c>.</summary>
        public static implicit operator InspectionSource(Reference<IInterlisDefinition> view) => new ViewRef { View = view };
    }

    /// <summary>An inspection written inline in the expression (<c>[AREA] INSPECTION OF Viewable -&gt; attr ...</c>), carrying the same formation an inspection view declares.</summary>
    public sealed class InlineInspection : InspectionSource
    {
        public required InspectionView Inspection { get; init; }
    }

    /// <summary>
    /// A reference to a named inspection view (<c>INSPECTION ViewableRef</c>). Resolution accepts any viewable —
    /// that the target actually is an inspection view is checked after resolution
    /// (see <see cref="CreateAST.Interlis24AstPathResolverVisitor"/>).
    /// </summary>
    public sealed class ViewRef : InspectionSource
    {
        public required Reference<IInterlisDefinition> View { get; init; }
    }
}
