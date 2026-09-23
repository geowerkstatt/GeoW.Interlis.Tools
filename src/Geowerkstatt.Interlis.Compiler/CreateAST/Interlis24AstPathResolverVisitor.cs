using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Resolves object/attribute paths (<see cref="PathExpression"/>, RefHB 3.13) after reference resolution. It walks
/// each path from the viewable it is written in — following attributes, roles, reference attributes, substructures
/// and view bases — and records the definition reached at its last element on <see cref="PathExpression.Target"/>
/// (for language-server go-to-definition). The tip type (<see cref="PathExpression.ReturnType"/>) is not stored: the
/// path derives it from that target and its last element.
/// <para>
/// It must run after the <see cref="Interlis24AstReferenceResolverVisitor"/> (it descends through resolved role /
/// reference targets and view bases) and before the type checker (whose constraint-condition check reads the resolved
/// tip type). Resolution is best-effort: an element it cannot resolve leaves the path's
/// <see cref="PathExpression.Target"/> <see langword="null"/> (and its <see cref="PathExpression.ReturnType"/> an
/// <see cref="ObjectType"/> / <see cref="UndefinedType"/>).
/// </para>
/// <para>
/// A member that does not exist on a <b>known</b> viewable is reported as an error — that is the compile-time
/// attribute-access check. Behind a multi-target role the object is of ONE of the targets, so a member must exist in
/// every target (checked as the intersection of the targets' members). No diagnostics are emitted when the context is
/// unknown (an unresolved previous element, a keyword such as <c>PARENT</c>, a named-domain alias, or a partially
/// typed element), so an undecidable path never turns an otherwise valid model into an error.
/// </para>
/// <para>
/// Named-domain aliases (<see cref="Types.TypeRef"/>) are transparent: descent — like the tip typing in
/// <see cref="PathExpression.ReturnType"/> — reads the domain's underlying type (<see cref="TypeDef.Underlying"/>),
/// so a value declared via a domain behaves like one declared with the inline type. An alias whose domain is
/// unresolved is treated as unknown.
/// </para>
/// </summary>
public class Interlis24AstPathResolverVisitor(ILoggerFactory loggerFactory) : Interlis24AstBaseVisitor<bool>
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Interlis24AstPathResolverVisitor>();

    /// <summary>
    /// The viewable a path head resolves against: the class / structure / association / view (or the
    /// <c>CONSTRAINTS OF</c> / <c>BASED ON</c> target) whose object the path is written for.
    /// </summary>
    private readonly Scope<IInterlisDefinitionContainer> currentViewable = new();

    protected internal override bool DefaultResult => true;

    protected internal override bool AggregateResult(bool aggregate, bool nextResult) => aggregate && nextResult;

    public override bool VisitModelDef([NotNull] ModelDef modelDef)
    {
        // The predefined INTERLIS model is hand-built with its path targets already set; do not traverse
        // (and re-resolve) it — its constraints use Annex A forms a parsed model could not resolve.
        if (modelDef == InternalModel.Interlis)
        {
            return DefaultResult;
        }

        return base.VisitModelDef(modelDef);
    }

    public override bool VisitClassDef([NotNull] ClassDef classDef)
    {
        using var frame = currentViewable.NewFrame(classDef);
        return base.VisitClassDef(classDef);
    }

    public override bool VisitAssociationDef([NotNull] AssociationDef associationDef)
    {
        using var frame = currentViewable.NewFrame(associationDef);
        return base.VisitAssociationDef(associationDef);
    }

    public override bool VisitViewDef([NotNull] ViewDef viewDef)
    {
        using var frame = currentViewable.NewFrame(viewDef);

        // The inspected path first: PARENT, THISAREA and THATAREA in the selections and attributes below denote the
        // object it leads through, read off its resolved steps (see KeywordContext).
        if (viewDef.Formation is InspectionView inspection)
        {
            ResolveInspectionPath(inspection);
        }

        // Selections (WHERE) and the AGGREGATION grouping paths are rooted at the view's base names (RefHB 3.15).
        foreach (var selection in viewDef.Selections)
        {
            ResolveExpression(selection, viewDef);
        }

        if (viewDef.Formation is AggregationView aggregation)
        {
            foreach (var path in aggregation.UniqueBy)
            {
                ResolvePath(path, viewDef);
            }
        }

        return base.VisitViewDef(viewDef);
    }

    public override bool VisitDomainDef([NotNull] DomainDef domainDef)
    {
        // Checked here rather than in VisitDomainConstraint: the diagnostics name the domain, and a
        // DomainConstraint carries no parent link to reach it from the constraint alone.
        foreach (var constraint in domainDef.TypeDef.Constraints.Values)
        {
            ResolveExpression(constraint.Condition, path => CheckDomainConstraintPath(path, domainDef, constraint));
        }

        return base.VisitDomainDef(domainDef);
    }

    public override bool VisitGraphicDef([NotNull] GraphicDef graphicDef)
    {
        // Every path in a graphic is rooted at the object of its BASED ON viewable (RefHB 3.16).
        var basedOn = graphicDef.BasedOn?.Target as IInterlisDefinitionContainer;
        using var frame = currentViewable.NewFrame(basedOn);

        foreach (var selection in graphicDef.Selections)
        {
            ResolveExpression(selection, basedOn);
        }

        foreach (var rule in graphicDef.DrawingRules)
        {
            foreach (var conditional in rule.Assignments)
            {
                ResolveExpression(conditional.Where, basedOn);
                foreach (var assignment in conditional.Assignments)
                {
                    ResolveExpression(assignment.Value, basedOn);
                    if (assignment.According != null)
                    {
                        ResolvePath(assignment.According, basedOn);
                    }
                }
            }
        }

        return base.VisitGraphicDef(graphicDef);
    }

    public override bool VisitConstraintsBlockDef([NotNull] ConstraintsBlockDef constraintsBlockDef)
    {
        // A CONSTRAINTS OF block's paths are rooted at the referenced viewable, not the enclosing topic (RefHB 3.12).
        using var frame = currentViewable.NewFrame(constraintsBlockDef.Target?.Target as IInterlisDefinitionContainer);
        return base.VisitConstraintsBlockDef(constraintsBlockDef);
    }

    public override bool VisitMandatoryConstraint([NotNull] MandatoryConstraint mandatoryConstraint)
    {
        ResolveExpression(mandatoryConstraint.Condition, currentViewable.Value);
        return base.VisitMandatoryConstraint(mandatoryConstraint);
    }

    public override bool VisitPlausibilityConstraint([NotNull] PlausibilityConstraint plausibilityConstraint)
    {
        ResolveExpression(plausibilityConstraint.Condition, currentViewable.Value);
        return base.VisitPlausibilityConstraint(plausibilityConstraint);
    }

    public override bool VisitSetConstraint([NotNull] SetConstraint setConstraint)
    {
        ResolveExpression(setConstraint.Where, currentViewable.Value);
        ResolveExpression(setConstraint.Condition, currentViewable.Value);
        return base.VisitSetConstraint(setConstraint);
    }

    public override bool VisitExistenceConstraint([NotNull] ExistenceConstraint existenceConstraint)
    {
        // The condition attribute path is rooted at the constrained viewable; each REQUIRED IN path is rooted at its
        // own ViewableRef (RefHB 3.12).
        ResolvePath(existenceConstraint.AttributePath, currentViewable.Value);
        foreach (var requirement in existenceConstraint.RequiredIn)
        {
            if (requirement.AttributePath != null)
            {
                ResolvePath(requirement.AttributePath, requirement.Viewable?.Target as IInterlisDefinitionContainer);
            }
        }

        return base.VisitExistenceConstraint(existenceConstraint);
    }

    public override bool VisitUniquenessConstraint([NotNull] UniquenessConstraint uniquenessConstraint)
    {
        ResolveExpression(uniquenessConstraint.Where, currentViewable.Value);
        foreach (var path in uniquenessConstraint.GlobalUnique)
        {
            ResolvePath(path, currentViewable.Value, mustBeSingleValued: true);
        }

        // The (LOCAL) form (RefHB 3.12): the structure path leads through substructure attributes of the enclosing
        // viewable, and the listed attributes are members of the reached substructure — resolved with the same
        // member lookup the path walk uses (an unknown step empties the container set, so followers stay silent
        // like in a path).
        if (uniquenessConstraint.Local is { } local)
        {
            IReadOnlyList<IInterlisDefinitionContainer> containers = currentViewable.Value is { } viewable ? [viewable] : [];
            foreach (var step in local.StructurePath.Path)
            {
                containers = DescendSubstructures(ResolveMemberStep(local.StructurePath, step, containers));
            }

            foreach (var attribute in local.AttributeNames)
            {
                // The listed attributes must be single-valued like global UNIQUE path elements; the structure
                // path above is exempt — leading through the collection is the point of the (LOCAL) form.
                CheckUniquePathElements(ResolveMemberStep(attribute, containers), containers, attribute.SourceRange);
            }
        }

        return base.VisitUniquenessConstraint(uniquenessConstraint);
    }

    public override bool VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        // Default / derived-value factors (RefHB 3.6.1-4/6, view attributes 3.15) are rooted at the enclosing viewable.
        foreach (var value in attributeDef.Values)
        {
            ResolveExpression(value, currentViewable.Value);
        }

        // ATTRIBUTE OF <path> (RefHB 3.8.11) leads to a class-typed attribute of the enclosing viewable.
        if (attributeDef.TypeDef is AttributePathType { Of: { } ofPath })
        {
            ResolvePath(ofPath, currentViewable.Value);
        }

        return base.VisitAttributeDef(attributeDef);
    }

    /// <summary>
    /// Recursively resolves every <see cref="PathExpression"/> nested in <paramref name="expression"/> against
    /// <paramref name="root"/> (the walk mirrors the collector the type checker uses for view-path checks), and
    /// checks the inspection factors it passes on the way (see <see cref="CheckInspectionSource"/>).
    /// </summary>
    private void ResolveExpression(IExpression? expression, IInterlisDefinitionContainer? root)
    {
        ResolveExpression(expression, path => ResolvePath(path, root));
    }

    /// <summary>
    /// Recursively finds every <see cref="PathExpression"/> nested in <paramref name="expression"/> and hands it
    /// to <paramref name="resolvePath"/> — the shared walk beneath the viewable-rooted resolution and the
    /// domain-constraint check, which differ only in what a path means. The inspection factors passed on the way
    /// are checked here (see <see cref="CheckInspectionSource"/>): an inline inspection's attribute path is rooted
    /// at its own source viewable, never the context (RefHB 3.13-48), while the <c>OF</c> restriction path is a
    /// context path like any other.
    /// </summary>
    private void ResolveExpression(IExpression? expression, Action<PathExpression> resolvePath)
    {
        switch (expression)
        {
            case PathExpression path:
                resolvePath(path);
                break;
            case BinaryExpression binary:
                ResolveExpression(binary.FirstOperand, resolvePath);
                ResolveExpression(binary.SecondOperand, resolvePath);
                break;
            case UnaryExpression unary:
                ResolveExpression(unary.Operand, resolvePath);
                break;
            case FunctionCall call:
                foreach (var argument in call.Arguments)
                {
                    ResolveExpression(argument, resolvePath);
                }

                break;
            case InspectionExpression inspection:
                CheckInspectionSource(inspection);
                if (inspection.Source is InspectionExpression.InlineInspection inline)
                {
                    ResolveInspectionPath(inline.Inspection);
                }

                ResolveExpression(inspection.Of, resolvePath);
                break;
        }
    }

    /// <summary>
    /// RefHB 3.13-25: the viewable referenced by a named inspection factor (<c>INSPECTION ViewableRef</c>) must be
    /// an inspection view — its defining formation (following <c>EXTENDS</c>) an <c>INSPECTION OF</c>. Reference
    /// resolution deliberately accepts any viewable so the mismatch is reported here by name; an unresolved
    /// reference stays silent (already reported by the reference resolver).
    /// </summary>
    private void CheckInspectionSource(InspectionExpression inspection)
    {
        if (inspection.Source is InspectionExpression.ViewRef { View: { Target: { } target } view }
            && EffectiveFormation(target as IInterlisDefinitionContainer) is not InspectionView)
        {
            logger.LogError("'{Viewable}' at {Range} can not be used as an INSPECTION factor because it is not an inspection view", target.FullyQualifiedName, view.GetRange());
        }
    }

    /// <summary>
    /// Reports the first path element a domain constraint can not use: everything but a bare <c>THIS</c>. The
    /// condition is an expression over the domain VALUE (RefHB 3.8-8/-10) — there is no context object, so the
    /// only resolvable path is the <c>THIS</c> denoting the value. ili2c agrees by construction: its
    /// domain-constraint grammar admits no path but <c>THIS</c> (probed 2026-08-29 — <c>THIS &gt; 10</c> accepted,
    /// <c>Unknown &gt; 1</c> and <c>THIS -&gt; Foo</c> rejected as parse errors).
    /// </summary>
    private void CheckDomainConstraintPath(PathExpression path, DomainDef domain, DomainConstraint constraint)
    {
        var steps = path.Reference.Path;
        var offending = steps is [KeywordPathSegment { Keyword: PathKeyword.This }, ..]
            ? steps.Count > 1 ? steps[1] : null
            : steps.FirstOrDefault();

        var name = offending?.Name ?? string.Empty;

        // An empty name is a partially typed element (the parse error is already reported) or no offender at all.
        if (name.Length > 0)
        {
            logger.LogError(
                "'{Element}' at {Range} can not be used in domain constraint '{Constraint}' of '{Domain}' because the condition can only refer to the domain value itself (THIS)",
                name,
                path.SourceRange ?? constraint.SourceRange ?? domain.GetNearestSourceRange(),
                constraint.Name,
                domain.FullyQualifiedName);
        }
    }

    /// <summary>
    /// Resolves and checks an inspection's attribute path (RefHB 3.15-15/-33), for a view formation and the
    /// inline expression factor alike, with the same member lookup a path walk uses (an unknown step empties the
    /// container set, so followers stay silent like in a path). Every step before the last must be a substructure
    /// attribute — the walk descends into its structure — and the reached tip must be decomposable: a
    /// substructure, or a single polyline / surface / area line attribute (a <c>MULTI...</c> geometry is one
    /// value, not a decomposable collection — RefHB 3.8.13.3-7). An <c>AREA INSPECTION</c> decomposes an area
    /// partition, so its tip must be an <c>AREA</c> attribute (RefHB 3.15-17). ili2c agrees on every rule
    /// (probed 2026-08-29).
    /// </summary>
    private void ResolveInspectionPath(InspectionView inspection)
    {
        var containers = InspectionRoots(inspection.Source.Viewable?.Target as IInterlisDefinitionContainer);
        var steps = inspection.Path.Path;

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var members = ResolveMemberStep(inspection.Path, step, containers);

            if (i < steps.Count - 1)
            {
                foreach (var member in members)
                {
                    if (InspectableType(member) is { } type && type is not ObjectType)
                    {
                        logger.LogError("the inspection path at {Range} can not continue after '{Member}' because it is not a substructure attribute", step.Range, member.FullyQualifiedName);
                    }
                }

                containers = DescendSubstructures(members);
            }
            else
            {
                foreach (var member in members)
                {
                    CheckInspectedTip(inspection.IsArea, member, step.Range);
                }
            }
        }
    }

    /// <summary>
    /// Resolves one plain-name step of a member path (a <see cref="LocalUniqueness"/> structure path, an
    /// <see cref="InspectionView"/> path) or a single member reference against the current container set with the
    /// same member lookup a path walk uses. Records the member on the step; when the step is the path's last, the
    /// path reaches it and it becomes the path's target. Returns the member definition(s) found — empty (and
    /// reported) when the name is missing somewhere or the context is unknown, so followers stay silent like in a
    /// path.
    /// </summary>
    private List<IInterlisDefinition> ResolveMemberStep(Reference<AttributeDef> path, PathSegment step, IReadOnlyList<IInterlisDefinitionContainer> containers)
    {
        var members = LookupInAll(containers, c => LookupMember(c, step.Name), step.Name, report: true, step.Range);
        if (members is [var member])
        {
            step.Target = member;
            if (ReferenceEquals(step, path.Path[^1]))
            {
                path.SetTarget(member);
            }
        }

        return members;
    }

    /// <summary>Resolves a single-segment <see cref="ReferenceResolution.Member"/> reference; see <see cref="ResolveMemberStep(Reference{AttributeDef}, PathSegment, IReadOnlyList{IInterlisDefinitionContainer})"/>.</summary>
    private List<IInterlisDefinition> ResolveMemberStep(Reference<AttributeDef> member, IReadOnlyList<IInterlisDefinitionContainer> containers)
        => member.Path is [var name] ? ResolveMemberStep(member, name, containers) : [];

    /// <summary>The substructure(s) the found member attribute(s) descend into; empty for any non-substructure member (see <see cref="DescendAll"/>).</summary>
    private static IReadOnlyList<IInterlisDefinitionContainer> DescendSubstructures(IReadOnlyList<IInterlisDefinition> members) =>
        DescendAll(members, member => member is AttributeDef { TypeDef: ObjectType substructure } ? DescendTargets(substructure) : []);

    /// <summary>Checks that the attribute an inspection path reaches can be decomposed (see <see cref="ResolveInspectionPath"/>).</summary>
    private void CheckInspectedTip(bool isArea, IInterlisDefinition member, RangePosition? range)
    {
        if (InspectableType(member) is not { } type)
        {
            return;
        }

        if (isArea)
        {
            if (type is not SurfaceType { IsCoverage: true, IsMultiGeometry: false })
            {
                logger.LogError("'{Member}' at {Range} can not be inspected by an AREA INSPECTION because its type is not an area partition (AREA)", member.FullyQualifiedName, range);
            }
        }
        else if (type is not (ObjectType or PolyLineType { IsMultiGeometry: false } or SurfaceType { IsMultiGeometry: false }))
        {
            logger.LogError("'{Member}' at {Range} can not be inspected because its type is not a substructure or a single polyline, surface or area", member.FullyQualifiedName, range);
        }
    }

    /// <summary>
    /// The viewable(s) an inspection path's first step resolves against: the source viewable itself, except when
    /// the source is an inspection view — its objects ARE the structure elements it yields (RefHB 3.15-15/-16),
    /// so the step names one of the elements' members, not one of the view's (the Anhang E Surface_Boundary2
    /// pattern: <c>INSPECTION OF Base ~ Surface_Boundary -&gt; Lines</c> steps into the predefined
    /// <c>SurfaceBoundary</c> structure the surface inspection yields — ili2c-probed 2026-08-29). ili2c resolves
    /// the step against the view's DECLARED attributes instead, with <c>ALL OF</c> materializing the element
    /// structure's; resolving against the elements directly accepts the same models plus an inspection view that
    /// omits the <c>ALL OF</c> (a leniency, not a stricter divergence).
    /// </summary>
    private static IReadOnlyList<IInterlisDefinitionContainer> InspectionRoots(IInterlisDefinitionContainer? source) =>
        EffectiveFormation(source) is InspectionView sourceInspection
            ? InspectedElements(sourceInspection)
            : source == null ? [] : [source];

    /// <summary>
    /// The structure(s) whose elements an inspection yields: the target structure(s) of a substructure tip, the
    /// predefined <c>SurfaceBoundary</c> for a plain inspection of a surface / area attribute (RefHB 3.15-16),
    /// the predefined <c>SurfaceEdge</c> for an <c>AREA INSPECTION</c> (RefHB 3.15-17). Empty when nothing
    /// definite is known: the tip is unresolved or invalid (already reported at the source view), or a polyline —
    /// its elements are the segment structures of its line forms, which is not a single lookup namespace.
    /// </summary>
    private static IReadOnlyList<IInterlisDefinitionContainer> InspectedElements(InspectionView inspection)
    {
        var tip = inspection.Path.Target;
        return (tip?.TypeDef.Underlying(), inspection.IsArea) switch
        {
            (ObjectType substructure, false) => DescendTargets(substructure),
            (SurfaceType { IsMultiGeometry: false }, false) => [(IInterlisDefinitionContainer)InternalModel.Interlis.Content["SurfaceBoundary"]],
            (SurfaceType { IsCoverage: true, IsMultiGeometry: false }, true) => [(IInterlisDefinitionContainer)InternalModel.Interlis.Content["SurfaceEdge"]],
            _ => [],
        };
    }

    /// <summary>
    /// The type an inspection path step is judged on: the member attribute's type with domain aliases seen
    /// through, or <see langword="null"/> when nothing definite is known — the member is no attribute (a view's
    /// base name) or its type is still unresolved or mid-typing — in which case the step checks stay silent so
    /// an incomplete model never produces a false positive.
    /// </summary>
    private static TypeDef? InspectableType(IInterlisDefinition member) =>
        (member as AttributeDef)?.TypeDef.Underlying() is { } type && type is not (UndefinedType or UnresolvedNamedType or TypeRef)
            ? type
            : null;

    /// <summary>
    /// Walks <paramref name="path"/> from <paramref name="root"/> (the viewable its head resolves against), recording
    /// what each step reached on its segment and what the last one reached as the reference's target — a rename of
    /// an attribute has to reach every step that names it, not only the paths ending on it. The tip type is not
    /// stored; <see cref="PathExpression.ReturnType"/> derives it from that target and the last step. A step that
    /// names no member of a known viewable is reported as an error. With <paramref name="mustBeSingleValued"/> every
    /// step must contribute at most one value (a UNIQUE path, see <see cref="CheckUniquePathElements"/>).
    /// </summary>
    private void ResolvePath(PathExpression path, IInterlisDefinitionContainer? root, bool mustBeSingleValued = false)
    {
        // containers: the viewables whose members the NEXT element resolves against — several once a multi-target
        // role was navigated, in which case the object is of ONE of them and a member must exist in each. Empty when
        // descent can not continue or the context is unknown.
        IReadOnlyList<IInterlisDefinitionContainer> containers = root == null ? [] : [root];
        IInterlisDefinition? reached = null;
        var steps = path.Reference.Path;

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];

            // The head of a path rooted at a view must name a base of the view — a stricter rule than membership,
            // reported by the type checker (CheckPathHead) — so an unknown view head is not reported here as well.
            var report = !(i == 0 && root is ViewDef);

            switch (step)
            {
                case KeywordPathSegment { Keyword: PathKeyword.This }:
                    // THIS is the context object; a following step continues from the same viewable(s).
                    reached = containers.Count == 1 ? containers[0] : null;
                    break;

                case KeywordPathSegment keyword:
                    // PARENT, THISAREA and THATAREA denote the object the inspected attribute belongs to, AGGREGATES
                    // the aggregated base objects: the view's formation says which viewable(s), so the walk continues
                    // from there. A keyword outside its context (reported for a head) denotes nothing and stops it.
                    if (i == 0)
                    {
                        CheckKeywordContext(keyword, root, path.SourceRange);
                    }

                    containers = KeywordContext(keyword, root);
                    reached = containers.Count == 1 ? containers[0] : null;
                    break;

                case RolePathSegment role:
                    var accesses = LookupInAll(containers, c => LookupRole(c, role.Name, role.Association.Name), role.ToString(), report, path.SourceRange);
                    var roles = accesses.Select(access => access.Role).ToList();
                    if (mustBeSingleValued)
                    {
                        CheckUniquePathElements(roles, containers, path.SourceRange);
                    }

                    reached = roles.Count == 1 ? roles[0] : null;
                    if (accesses is [var access])
                    {
                        // The step writes two names; both point into the access the role was found through.
                        role.Target = access.Role;
                        role.Association.Target = access.Association;
                    }

                    containers = DescendAll(roles, member => DescendTargets((member as AttributeDef)?.TypeDef));
                    break;

                case IndexedPathSegment indexed:
                    var indexedAttributes = LookupInAll(containers, c => LookupMember(c, indexed.Name) as AttributeDef, indexed.Name, report, path.SourceRange);
                    if (mustBeSingleValued)
                    {
                        CheckUniquePathElements(indexedAttributes, containers, path.SourceRange);
                    }

                    reached = indexedAttributes.Count == 1 ? indexedAttributes[0] : null;
                    indexed.Target = reached;

                    // An indexed step only descends into the element structure of an ordered LIST OF substructure
                    // (a coordinate axis or a non-indexable attribute yields no viewable).
                    containers = DescendSubstructures(indexedAttributes);
                    break;

                default:
                    var members = LookupInAll(containers, c => LookupMember(c, step.Name), step.Name, report, path.SourceRange);
                    if (mustBeSingleValued)
                    {
                        CheckUniquePathElements(members, containers, path.SourceRange);
                    }

                    reached = members.Count == 1 ? members[0] : null;
                    step.Target = reached;
                    containers = DescendAll(members, DescendMember);
                    break;
            }
        }

        // A THIS tip reaches the context viewable, which SetTarget leaves off the keyword segment.
        if (reached != null)
        {
            path.Reference.SetTarget(reached);
        }
    }

    /// <summary>
    /// The viewable(s) whose object a keyword step denotes, from the formation of the view the path is written in:
    /// for PARENT, THISAREA and THATAREA the object the inspected attribute belongs to (RefHB 3.13-35/-36), for
    /// AGGREGATES the aggregated objects of the aggregation's source (RefHB 3.13-43). Empty where the keyword has no
    /// such context — misuse, which <see cref="CheckKeywordContext"/> reports, or an unresolved source.
    /// </summary>
    private static IReadOnlyList<IInterlisDefinitionContainer> KeywordContext(KeywordPathSegment keyword, IInterlisDefinitionContainer? root) => (keyword.Keyword, EffectiveFormation(root)) switch
    {
        (PathKeyword.Parent, InspectionView inspection) => InspectedParents(inspection),
        (PathKeyword.ThisArea or PathKeyword.ThatArea, InspectionView { IsArea: true } inspection) => InspectedParents(inspection),
        (PathKeyword.Aggregates, AggregationView aggregation) => aggregation.Source.Viewable?.Target is IInterlisDefinitionContainer viewable ? [viewable] : [],
        _ => [],
    };

    /// <summary>
    /// The viewable(s) whose object the inspected attribute belongs to: the source viewable for a one-step path (or
    /// the elements of a source that is itself an inspection, see <see cref="InspectionRoots"/>), the substructure
    /// the previous step reaches for a longer one. Empty while a step is unresolved.
    /// </summary>
    private static IReadOnlyList<IInterlisDefinitionContainer> InspectedParents(InspectionView inspection)
    {
        var steps = inspection.Path.Path;
        return steps.Count switch
        {
            0 => [],
            1 => InspectionRoots(inspection.Source.Viewable?.Target as IInterlisDefinitionContainer),
            _ => steps[^2].Target is AttributeDef { TypeDef: ObjectType substructure } ? DescendTargets(substructure) : [],
        };
    }

    /// <summary>
    /// The context rules of the keyword path elements: THISAREA/THATAREA are only usable within the inspection of
    /// an area partition (RefHB 3.13-35) and AGGREGATES only within an aggregation view (RefHB 3.13-43). Checked
    /// for the HEAD position only: a keyword deeper in a path rides on an already-established context object
    /// (e.g. <c>THIS-&gt;PARENT-&gt;...</c>), which ili2c tolerates too — PARENT's inspection-only rule
    /// (RefHB 3.13-36) is therefore not enforced at all. The view kind is taken from the formation, following a
    /// view's EXTENDS chain to the defining base.
    /// </summary>
    private void CheckKeywordContext(KeywordPathSegment keyword, IInterlisDefinitionContainer? root, RangePosition? range)
    {
        switch (keyword.Keyword)
        {
            case PathKeyword.ThisArea or PathKeyword.ThatArea
                when EffectiveFormation(root) is not InspectionView { IsArea: true }:
                ReportKeywordContext(keyword, root, range, "the inspection of an area partition");
                break;

            case PathKeyword.Aggregates when EffectiveFormation(root) is not AggregationView:
                ReportKeywordContext(keyword, root, range, "an aggregation view");
                break;
        }
    }

    private void ReportKeywordContext(KeywordPathSegment keyword, IInterlisDefinitionContainer? root, RangePosition? range, string requiredContext)
    {
        logger.LogError(
            "'{Keyword}' at {Range} can only be used within {RequiredContext}{Where}",
            keyword.Name,
            range,
            requiredContext,
            root is IInterlisDefinition definition ? $" (in '{definition.FullyQualifiedName}')" : string.Empty);
    }

    /// <summary>
    /// The formation that defines how the viewable is formed: the view's own formation or, for a view that
    /// <c>EXTENDS</c> another instead, the base chain's (cycle-guarded); <see langword="null"/> for classes and
    /// unresolved views.
    /// </summary>
    private static ViewFormation? EffectiveFormation(IInterlisDefinitionContainer? root)
    {
        var visited = new HashSet<ViewDef>();
        for (var view = root as ViewDef; view != null && visited.Add(view); view = view.Extends?.Target)
        {
            if (view.Formation != null)
            {
                return view.Formation;
            }
        }

        return null;
    }

    /// <summary>
    /// RefHB 3.12: every element of a <c>UNIQUE</c> attribute path must contribute a single value, so an attribute
    /// owning a collection (maximum cardinality above 1) or a role navigation that can reach several links can not
    /// appear in the path (ili2c agrees: "unexpected cardinality of attribute ..."). The roles OF the constrained
    /// association itself are exempt — a link instance holds exactly one target per role — matching ili2c, which
    /// only checks class-side role navigations.
    /// </summary>
    private void CheckUniquePathElements(IEnumerable<IInterlisDefinition> members, IReadOnlyList<IInterlisDefinitionContainer> containers, RangePosition? range)
    {
        foreach (var member in members)
        {
            if (member is not AttributeDef attribute || IsSingleValued(attribute.TypeDef.Cardinality))
            {
                continue;
            }

            var isRole = attribute.TypeDef is RoleType;
            if (isRole && attribute.Parent is { } declaringAssociation && containers.Contains(declaringAssociation))
            {
                continue; // a role of the constrained association itself
            }

            logger.LogError(
                "The {Kind} '{Member}' at {Range} can not be used in a UNIQUE constraint because its maximum cardinality is above 1",
                isRole ? "role" : "attribute",
                attribute.FullyQualifiedName,
                range);
        }
    }

    /// <summary>
    /// Whether the cardinality admits at most one value. A missing cardinality (a bare inherited type reference
    /// after a syntax error) counts as single-valued so it never produces a spurious error.
    /// </summary>
    private static bool IsSingleValued(Cardinality? cardinality) => cardinality == null || cardinality.Max is <= 1;

    /// <summary>
    /// Looks up a path element in every container of the current set, reporting it if it is missing from any (the
    /// object is of ONE of the containers, so a member is only valid if each has it). Returns the distinct member
    /// definitions found — a single definition when all containers agree (e.g. an attribute inherited from a common
    /// base) — or an empty list when the element is missing somewhere or the context is unknown (no containers).
    /// Reporting is suppressed for an empty <paramref name="name"/> (a partially typed element, e.g. a trailing
    /// <c>-&gt;</c> — the parse error is already reported) and when <paramref name="report"/> is
    /// <see langword="false"/> (a view head, whose stricter rule the type checker reports).
    /// </summary>
    private List<T> LookupInAll<T>(IReadOnlyList<IInterlisDefinitionContainer> containers, Func<IInterlisDefinitionContainer, T?> lookup, string name, bool report, RangePosition? range)
        where T : class
    {
        var members = new List<T>();
        var missing = new List<IInterlisDefinitionContainer>();
        foreach (var container in containers)
        {
            var member = lookup(container);
            if (member == null)
            {
                missing.Add(container);
            }
            else if (!members.Contains(member))
            {
                members.Add(member);
            }
        }

        if (missing.Count > 0)
        {
            if (report && name.Length > 0)
            {
                logger.LogError("Could not resolve '{Name}' in '{Containers}' at {Range}", name, string.Join("', '", missing.Select(c => c.FullyQualifiedName)), range);
            }

            return [];
        }

        return members;
    }

    /// <summary>
    /// The union of the viewables the found member definition(s) navigate into. Empty — stopping both descent and
    /// further checking — as soon as any member yields no viewable, so a scalar branch or an only partially known
    /// target set never produces a false error on a later element.
    /// </summary>
    private static IReadOnlyList<IInterlisDefinitionContainer> DescendAll(IReadOnlyList<IInterlisDefinition> members, Func<IInterlisDefinition, IReadOnlyList<IInterlisDefinitionContainer>> descend)
    {
        var result = new List<IInterlisDefinitionContainer>();
        foreach (var member in members)
        {
            var targets = descend(member);
            if (targets.Count == 0)
            {
                return [];
            }

            result.AddRange(targets.Where(target => !result.Contains(target)));
        }

        return result;
    }

    /// <summary>
    /// The viewable(s) a bare-name member navigates into: an object-valued attribute descends into its type's
    /// target(s), a base view into its referenced viewable — or, for the base of an <c>INSPECTION</c>, into the
    /// inspected structure elements (see <see cref="InspectedBy"/>) — a class / structure / association / view into
    /// itself.
    /// </summary>
    private static IReadOnlyList<IInterlisDefinitionContainer> DescendMember(IInterlisDefinition member) => member switch
    {
        AttributeDef attribute => DescendTargets(attribute.TypeDef),
        BaseView baseView when InspectedBy(baseView) is { } inspection => InspectedElements(inspection),
        BaseView baseView => baseView.Viewable?.Target is IInterlisDefinitionContainer viewable ? [viewable] : [],
        IInterlisDefinitionContainer viewable => [viewable],
        _ => [],
    };

    /// <summary>
    /// The inspection whose elements <paramref name="baseView"/> stands for, or <see langword="null"/> for the base
    /// of any other formation. An <c>INSPECTION OF base ~ Viewable -&gt; Attr</c> yields one object per structure
    /// element, and the base name denotes that element (RefHB 3.15-33): <c>base-&gt;Member</c> is a member of the
    /// element structure, while the containing object of the viewable is reached through <c>PARENT</c> (see
    /// <see cref="KeywordContext"/>). ili2c agrees: it accepts a member of the element structure after the base name.
    /// </summary>
    private static InspectionView? InspectedBy(BaseView baseView) =>
        baseView.Parent is ViewDef { Formation: InspectionView inspection } && inspection.Source == baseView ? inspection : null;

    /// <summary>
    /// The viewable(s) an object-valued type navigates into: every target of a role, the target of a reference or
    /// substructure, the restriction(s) of a <c>CLASS</c> domain. Empty if the type is scalar or any of its targets
    /// is unresolved or an <c>ANY…</c> placeholder — a partially known target set can not be checked.
    /// </summary>
    private static IReadOnlyList<IInterlisDefinitionContainer> DescendTargets(TypeDef? type)
    {
        // A named-domain alias is transparent, e.g. an attribute typed by a metaobject CLASS domain descends into
        // the domain's restrictions like an inline CLASS attribute does.
        type = type?.Underlying();

        List<IInterlisDefinitionContainer?> targets = type switch
        {
            RoleType role => role.Targets.Select(TargetOf).ToList(),
            ReferenceType reference => [TargetOf(reference.Target)],
            ObjectType substructure => substructure.Targets.Select(TargetOf).ToList(),
            ClassType classType => classType.Restrictions.Select(restriction => restriction.Target as IInterlisDefinitionContainer).ToList(),
            _ => [],
        };

        return targets.Count > 0 && targets.All(target => target != null)
            ? targets.Cast<IInterlisDefinitionContainer>().Distinct().ToList()
            : [];
    }

    /// <summary>The resolved target of a <see cref="RestrictedRef"/>, or <see langword="null"/> for an unresolved reference or an <c>ANY…</c> placeholder.</summary>
    private static IInterlisDefinitionContainer? TargetOf(RestrictedRef restrictedRef) =>
        (restrictedRef.Value as RestrictedRef.DefinitionRef)?.Reference.Target as IInterlisDefinitionContainer;

    /// <summary>
    /// Looks up a bare name as a member of <paramref name="container"/> — an attribute or base, or (for a class /
    /// association) a role reached through one of its association accesses (RefHB 3.7.5) — walking the <c>EXTENDS</c>
    /// base chain. Returns <see langword="null"/> if no member matches.
    /// </summary>
    private static IInterlisDefinition? LookupMember(IInterlisDefinitionContainer? container, string name)
    {
        foreach (var current in SelfAndBases(container))
        {
            if (current.Content.TryGetValue(name, out var member))
            {
                return member;
            }

            if (current is IIdentifiable identifiable)
            {
                foreach (var association in identifiable.AssociationAccess.Values)
                {
                    if (association.Content.GetValueOrDefault(name) is AttributeDef { TypeDef: RoleType } role)
                    {
                        return role;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// A role reached through one of a class's association accesses ("Beziehungszugang", RefHB 2.7): the role and
    /// the association granting the access, the two definitions a <c>Role-Name '[' Association-Name ']'</c> step
    /// names.
    /// </summary>
    private sealed record RoleAccess(AttributeDef Role, AssociationDef Association);

    /// <summary>Resolves a <c>Role-Name '[' Association-Name ']'</c> step against the current object's association accesses (RefHB 3.13-38).</summary>
    private static RoleAccess? LookupRole(IInterlisDefinitionContainer? container, string roleName, string associationName)
    {
        foreach (var current in SelfAndBases(container))
        {
            if (current is IIdentifiable identifiable
                && identifiable.AssociationAccess.GetValueOrDefault(associationName) is { } association
                && association.Content.GetValueOrDefault(roleName) is AttributeDef { TypeDef: RoleType } role)
            {
                return new RoleAccess(role, association);
            }
        }

        return null;
    }

    /// <summary>
    /// A viewable and the containers it inherits member names from, breadth-first (nearest bases first, so a
    /// locally defined member shadows an inherited one): the <c>EXTENDS</c> base chain (RefHB 3.5.4-11) and, for
    /// a view, also the base viewables whose attributes are taken over with <c>ALL OF</c> (RefHB 3.15). Guards
    /// against an extension cycle so member lookup can not loop — the type checker diagnoses cycles, but this
    /// pass runs before it, so every chain walk must protect itself.
    /// </summary>
    private static IEnumerable<IInterlisDefinitionContainer> SelfAndBases(IInterlisDefinitionContainer? container)
    {
        var visited = new HashSet<IInterlisDefinitionContainer>();
        var pending = new Queue<IInterlisDefinitionContainer>();
        if (container != null)
        {
            pending.Enqueue(container);
        }

        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }

            yield return current;

            foreach (var baseContainer in BasesOf(current))
            {
                pending.Enqueue(baseContainer);
            }
        }
    }

    /// <summary>
    /// The base containers whose member names <paramref name="container"/> inherits: the <c>EXTENDS</c> target of
    /// a class, association or view, and for a view also the <c>ALL OF</c> base viewables. This pass runs after
    /// the reference resolver, so the base references are simply read; an unresolved base contributes nothing.
    /// </summary>
    private static IEnumerable<IInterlisDefinitionContainer> BasesOf(IInterlisDefinitionContainer container)
    {
        switch (container)
        {
            case ClassDef { Extends.Target: { } baseClass }:
                yield return baseClass;
                break;

            case AssociationDef { Extends.Target: { } baseAssociation }:
                yield return baseAssociation;
                break;

            case ViewDef viewDef:
                if (viewDef.Extends?.Target is { } baseView)
                {
                    yield return baseView;
                }

                foreach (var allOf in viewDef.AllOfBases)
                {
                    if (allOf.Target is { Viewable.Target: IInterlisDefinitionContainer allOfViewable })
                    {
                        yield return allOfViewable;
                    }
                }

                break;
        }
    }
}
