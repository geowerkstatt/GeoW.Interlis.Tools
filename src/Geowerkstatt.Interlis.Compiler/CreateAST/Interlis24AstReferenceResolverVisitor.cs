using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Resolves various references inside the AST. The AST is modified in place.
/// </summary>
public class Interlis24AstReferenceResolverVisitor(ILoggerFactory loggerFactory) : Interlis24AstBaseVisitor<bool>
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Interlis24AstReferenceResolverVisitor>();
    private readonly Scope<InterlisEnvironment> currentEnvironment = new();

    /// <summary>
    /// The running constraint counter per viewable, used to continue the <see cref="ConstraintDef.NameIndex"/>
    /// numbering across all <c>CONSTRAINTS OF</c> blocks attached to the same viewable.
    /// </summary>
    private readonly Dictionary<IConstraintContainer, int> externalConstraintIndices = new();

    /// <summary>
    /// References currently being resolved. Base containers are resolved on demand during a name lookup
    /// (<see cref="BaseOf"/>), which can re-enter <see cref="Resolve{T}"/> for the very reference whose lookup is in
    /// progress (a topic's own <c>EXTENDS</c> passes through that topic's scope level); the re-entrant call must
    /// yield "unknown" instead of recursing.
    /// </summary>
    private readonly HashSet<IReference> resolving = new();

    /// <summary>
    /// References that already failed to resolve, so an on-demand attempt (<see cref="BaseOf"/>) and the regular
    /// visit do not log the same error twice.
    /// </summary>
    private readonly HashSet<IReference> failed = new();

    /// <summary>
    /// Resolves the reference. If successful, the <see cref="Reference{T}.Target"/> is set accordingly.
    /// Accepts <see langword="null"/>: an optional clause the parser could not build (a mandatory one missing
    /// after a syntax error, an absent one) has nothing to resolve.
    /// </summary>
    /// <returns><see langword="true"/> if the <paramref name="reference"/> was resolved successfully, <see langword="false"/> otherwise.</returns>
    private bool Resolve<T>(Reference<T>? reference) where T : class, IReferenceTarget
    {
        if (reference == null || reference.Source == null)
        {
            // Nothing to resolve
            return true;
        }

        if (reference.Target != null)
        {
            // Already resolved
            return true;
        }

        if (failed.Contains(reference) || !resolving.Add(reference))
        {
            return false;
        }

        try
        {
            if (reference.Path.Count == 0)
            {
                // References without a path can occur after parse errors, the offending input already has a compile error logged.
                logger.LogError("Could not resolve '{Reference}' at {Range}", reference, reference.GetRange());
                failed.Add(reference);
                return false;
            }

            var mappedTargets = CollectPotentialTargets(reference.Source!, reference.Path, resolveModelInEnvironment: reference.ResolvesInEnvironment)
                .Where(reference.CanAccept)
                .ToList();

            switch (mappedTargets.Count)
            {
                case 0:
                    logger.LogError("Could not resolve '{Reference}' at {Range}", reference, reference.GetRange());
                    failed.Add(reference);
                    return false;

                case 1:
                    reference.SetTarget(mappedTargets.Single());
                    ReportMissingModelQualification(reference);
                    return true;

                default:
                    logger.LogError("Ambiguous '{Reference}' at {Range} could be resolved to multiple targets: {Targets}", reference, reference.GetRange(), string.Join(", ", mappedTargets.Select(d => d.FullyQualifiedName)));
                    failed.Add(reference);
                    return false;
            }
        }
        finally
        {
            resolving.Remove(reference);
        }
    }

    /// <summary>
    /// Reports a reference that resolved but is not qualified as the RefHB requires: a reference must be either an
    /// unqualified single name or fully qualified as <c>Model[.Topic].Name</c>. A qualification that starts at an
    /// enclosing topic (e.g. <c>Topic.X</c>, or an attribute-path constant's viewable in <c>Topic.Class-&gt;attr</c>)
    /// is resolved relatively for convenience so later stages have the target, but the model name is missing; this is
    /// logged as an error so the user adds it.
    /// </summary>
    private void ReportMissingModelQualification<T>(Reference<T> reference) where T : class, IReferenceTarget
    {
        // The qualification is the whole path for a general reference; for an attribute-path constant
        // (Reference<AttributeDef>) it is the viewable prefix, since the trailing segment is the attribute member.
        var qualificationLength = typeof(T) == typeof(AttributeDef) ? reference.Path.Count - 1 : reference.Path.Count;
        if (qualificationLength <= 1)
        {
            // Unqualified single name (or bare attribute) — always valid.
            return;
        }

        var model = FindRoot<ModelDef>(reference.Source);
        var head = reference.Path[0];
        if (model == null || head == model.Name || model.Imports.ContainsKey(head))
        {
            // Fully qualified from the model itself or an imported model — valid.
            return;
        }

        logger.LogError("'{Reference}' at {Range} must be fully qualified with its model name.", reference, reference.GetRange());
    }

    /// <summary>
    /// The outermost ancestor of type <typeparamref name="T"/> in the <see cref="IInterlisDefinition.Parent"/> chain of
    /// <paramref name="element"/> (inclusive), or <see langword="null"/> if there is none. Topics and models never nest,
    /// so there is at most one such ancestor: the enclosing <see cref="TopicDef"/> or the root <see cref="ModelDef"/>.
    /// </summary>
    private static T? FindRoot<T>(IInterlisDefinition? element) where T : class
    {
        T? root = null;
        for (var current = element; current != null; current = current.Parent)
        {
            if (current is T match)
            {
                root = match;
            }
        }

        return root;
    }

    /// <summary>
    /// Collects the definitions <paramref name="path"/> could resolve to, starting from <paramref name="source"/>:
    /// relative to the enclosing scope (a single name, or a longer path descending into the element found there) or
    /// fully qualified from the root model or an import. Relative multi-segment resolution is a convenience — the
    /// resulting reference is flagged by <see cref="ReportMissingModelQualification"/> as missing its model name.
    /// When <paramref name="resolveModelInEnvironment"/> is set, a single-segment path is also looked up as a model in
    /// the current environment (used to resolve <see cref="ModelDef"/> references such as imports).
    /// </summary>
    private List<IInterlisDefinition> CollectPotentialTargets(IInterlisDefinitionContainer source, IReadOnlyList<string> path, bool resolveModelInEnvironment)
    {
        var potentialTargets = new List<IInterlisDefinition>();

        // Resolve relative to the enclosing scope chain: a single name is the element found there; a longer path
        // descends into it (an attribute-path constant '>>Class->attr', or a topic-relative 'Topic.X'). Each scope
        // level also provides the names inherited from its EXTENDS chain (RefHB 3.5.4-11).
        IInterlisDefinitionContainer? current = source;
        IInterlisDefinitionContainer root = source;
        while (current != null)
        {
            root = current;
            if (LookupIncludingBases(current, path[0]) is { } element)
            {
                potentialTargets.AddIfNotNull(path.Count == 1 ? element : ResolveDescending(element, path));
            }

            current = current.Parent;
        }

        // At the root is always a model if a complete interlis model was parsed.
        if (root is ModelDef model)
        {
            // A path of type ModelDef is resolved in the current environment (import statements, translation of).
            if (path.Count == 1 && currentEnvironment.Value != null && resolveModelInEnvironment)
            {
                potentialTargets.AddIfNotNull(currentEnvironment.Value.Content.GetValueOrDefault(path[0]));
            }
            else
            {
                // Fully qualified from the root model.
                if (path[0] == model.Name)
                {
                    potentialTargets.AddIfNotNull(ResolveDescending(model, path));
                }

                // Fully qualified in an imported model.
                if (model.Imports.TryGetValue(path[0], out var import))
                {
                    potentialTargets.AddIfNotNull(ResolveDescending(import.ModelDef.Target, path));
                }

                // Unqualified in an import that allows it.
                if (path.Count == 1)
                {
                    foreach (var unqualifiedImport in model.Imports.Values.Where(m => m.IsUnqualifiedAllowed))
                    {
                        if (unqualifiedImport.ModelDef?.Target?.Content.TryGetValue(path[0], out var element) == true)
                        {
                            potentialTargets.Add(element);
                        }
                    }
                }
            }
        }

        return potentialTargets;
    }

    /// <summary>
    /// Resolves the remaining path segments (<c>path[1..]</c>) by descending through the container content —
    /// including the names each container inherits from its <c>EXTENDS</c> chain (RefHB 3.5.4-11) — starting at
    /// <paramref name="start"/>.
    /// </summary>
    private IInterlisDefinition? ResolveDescending(IInterlisDefinition? start, IReadOnlyList<string> path)
    {
        IInterlisDefinition? target = start;
        for (var i = 1; i < path.Count; i++)
        {
            target = target is IInterlisDefinitionContainer container ? LookupIncludingBases(container, path[i]) : null;
            if (target == null)
            {
                return null;
            }
        }

        return target;
    }

    /// <summary>
    /// Looks up a name in the container, including the names it inherits: extending a modelling element adds all
    /// names of the base element to its namespaces (RefHB 3.5.4-11), so a topic sees the definitions of its base
    /// topic chain and a class/association the members of its base chain; a view additionally takes over the
    /// attributes of the base viewables named in <c>ALL OF</c> (RefHB 3.15). A locally defined name shadows the
    /// inherited one (breadth-first, nearest bases first). Cycle-guarded; an unresolved base contributes nothing.
    /// </summary>
    private IInterlisDefinition? LookupIncludingBases(IInterlisDefinitionContainer container, string name)
    {
        var visited = new HashSet<IInterlisDefinitionContainer>();
        var pending = new Queue<IInterlisDefinitionContainer>();
        pending.Enqueue(container);
        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }

            if (current.Content.TryGetValue(name, out var element))
            {
                return element;
            }

            foreach (var baseContainer in BasesOf(current))
            {
                pending.Enqueue(baseContainer);
            }
        }

        return null;
    }

    /// <summary>
    /// The base containers whose names a container inherits: the <c>EXTENDS</c> base of a topic, class,
    /// association or view, and for a view also the base viewables whose attributes are taken over with
    /// <c>ALL OF</c>. A <c>CONSTRAINTS OF</c> block contributes its target viewable — the block declares no names
    /// of its own and its constraints resolve against the viewable they are attached to (RefHB 3.12-41). Base
    /// references are resolved on demand: a base living in another model may not have been visited yet when a
    /// name inside the extending element is looked up (models are resolved in load order, imports after the
    /// importing model).
    /// </summary>
    private IEnumerable<IInterlisDefinitionContainer> BasesOf(IInterlisDefinitionContainer container)
    {
        switch (container)
        {
            case ConstraintsBlockDef { Target: { } blockTarget }:
                Resolve(blockTarget);
                if (blockTarget.Target is IInterlisDefinitionContainer targetViewable)
                {
                    yield return targetViewable;
                }

                break;

            case TopicDef { Extends: { } topicBase }:
                Resolve(topicBase);
                if (topicBase.Target is { } baseTopic)
                {
                    yield return baseTopic;
                }

                break;

            case ClassDef { Extends: { } classBase }:
                Resolve(classBase);
                if (classBase.Target is { } baseClass)
                {
                    yield return baseClass;
                }

                break;

            case AssociationDef { Extends: { } associationBase }:
                Resolve(associationBase);
                if (associationBase.Target is { } baseAssociation)
                {
                    yield return baseAssociation;
                }

                break;

            case ViewDef viewDef:
                if (viewDef.Extends is { } viewBase)
                {
                    Resolve(viewBase);
                    if (viewBase.Target is { } baseView)
                    {
                        yield return baseView;
                    }
                }

                foreach (var allOf in viewDef.AllOfBases)
                {
                    Resolve(allOf);
                    if (allOf.Target is { Viewable: { } viewable })
                    {
                        Resolve(viewable);
                        if (viewable.Target is IInterlisDefinitionContainer allOfViewable)
                        {
                            yield return allOfViewable;
                        }
                    }
                }

                break;
        }
    }

    protected internal override bool DefaultResult => true;

    protected internal override bool AggregateResult(bool aggregate, bool nextResult)
    {
        return aggregate && nextResult;
    }

    public override bool VisitInterlisEnvironment([NotNull] InterlisEnvironment interlisEnvironment)
    {
        using var scopeFrame = currentEnvironment.NewFrame(interlisEnvironment);

        // Import (and translation-base) references anchor every cross-model lookup. On-demand base resolution
        // (BasesOf) can descend into a model BEFORE that model's own visit — e.g. resolving a name inside an
        // extending topic walks the base topic's classes and demands their EXTENDS references — and those
        // resolutions fail spuriously (and stick, via the failed set) when the target model's imports are not
        // resolved yet. Resolving all import references up front makes on-demand resolution independent of the
        // model visit order.
        foreach (var model in interlisEnvironment.Content.Values.OfType<ModelDef>())
        {
            foreach (var (_, importReference) in model.Imports.Values)
            {
                Resolve(importReference);
            }

            if (model.TranslationOf is { } translationOf)
            {
                Resolve(translationOf);
                if (translationOf.Target is ModelDef translationOriginal)
                {
                    LinkTranslationElements(model, translationOriginal);
                }
            }
        }

        return base.VisitInterlisEnvironment(interlisEnvironment);
    }

    /// <summary>
    /// Writes the element-level translation links of a translated container: a translation may only change names
    /// (RefHB 3.5.1-10), so its elements correspond to the original's elements by declaration position. Each
    /// element gets an unregistered reference to its original (see <see cref="IInterlisDefinition.TranslationOf"/>)
    /// and nested containers are linked recursively. Where the containers diverge structurally (different counts
    /// or kinds) the linking stops at the first mismatch; reporting such divergences is up to the type checker.
    /// </summary>
    private static void LinkTranslationElements(IInterlisDefinitionContainer container, IInterlisDefinitionContainer original)
    {
        foreach (var (element, originalElement) in container.InDeclarationOrder().Zip(original.InDeclarationOrder()))
        {
            if (element.GetType() != originalElement.GetType())
            {
                return;
            }

            element.TranslationOf = new Reference<IInterlisDefinition> { Target = originalElement };
            if (element is IInterlisDefinitionContainer childContainer && originalElement is IInterlisDefinitionContainer originalChildContainer)
            {
                LinkTranslationElements(childContainer, originalChildContainer);
            }
        }
    }

    public override bool VisitDomainDef([NotNull] DomainDef domainDef)
    {
        LinkRefSystems(domainDef.TypeDef);
        LinkFormatAttributes(domainDef.TypeDef);
        return base.VisitDomainDef(domainDef);
    }

    /// <summary>
    /// Links the attribute references of a <c>FORMAT BASED ON</c> definition to the members of its base structure
    /// (including inherited ones): the format's attributes are members of the <c>BASED ON</c> target, not scoped
    /// names, so their unregistered references are linked here like the other anchored lookups (meta objects,
    /// basket classes). Unknown names stay unresolved without a report — format validation is a separate concern,
    /// the linked attribute serves navigation.
    /// </summary>
    private void LinkFormatAttributes(TypeDef type)
    {
        if (type is not FormattedType { BasedOn: { } basedOnReference, Format: { } format })
        {
            return;
        }

        Resolve(basedOnReference);
        if (basedOnReference.Target is not IInterlisDefinitionContainer basedOn)
        {
            return;
        }

        foreach (var component in format.Components.OfType<FormatBaseAttribute>())
        {
            if (component.Attribute is { Target: null, Path: [{ } attributeName] }
                && LookupIncludingBases(basedOn, attributeName) is AttributeDef found)
            {
                component.Attribute.SetTarget(found);
            }
        }
    }

    /// <summary>
    /// Resolves the meta-object link of the reference systems of a numeric type or the axes of a coordinate type:
    /// the declared name the <c>{basket.metaObject}</c> form references is not an <see cref="IInterlisDefinition"/>
    /// (a meta object is data, its declared name in the basket is its model-world anchor), so it is not registered
    /// for scoped resolution — the target of <see cref="RefSys.MetaObjectRef.MetaObject"/> is written here instead, searching the
    /// basket and its inherited definitions in the runtime order (RefHB 3.10.1-3). The basket itself is resolved on
    /// demand (it may live in a model visited later).
    /// </summary>
    private void LinkRefSystems(TypeDef type)
    {
        switch (type)
        {
            case NumericType { RefSystem: { } refSystem }:
                LinkMetaObject(refSystem);
                break;

            case CoordType coord:
                foreach (var axis in coord.Axis)
                {
                    if (axis.RefSystem is { } axisRefSystem)
                    {
                        LinkMetaObject(axisRefSystem);
                    }
                }

                break;
        }
    }

    private void LinkMetaObject(RefSys refSystem)
    {
        if (refSystem.Value is not RefSys.MetaObjectRef { Basket: { } basketReference, MetaObject: { Target: null, Path: [{ } metaObjectName] } metaObject })
        {
            return;
        }

        Resolve(basketReference);

        var visited = new HashSet<MetaDataBasketDef>();
        for (var basket = basketReference.Target; basket != null && visited.Add(basket); basket = basket.Extends?.Target)
        {
            foreach (var objects in basket.Objects)
            {
                if (objects.MetaObjects.FirstOrDefault(declaration => declaration.Name == metaObjectName) is { } found)
                {
                    metaObject.Target = found;
                    return;
                }
            }
        }
    }

    public override bool VisitMetaDataBasketDef([NotNull] MetaDataBasketDef metaDataBasketDef)
    {
        // The OBJECTS OF classes live in the basket's topic (RefHB 3.10.1-6), not the enclosing scope, so their
        // unregistered references are linked here against the topic's (inherited) content. Unknown names stay
        // unresolved without a report — the reference tool is lenient about basket contents, and the linked class
        // serves navigation.
        Resolve(metaDataBasketDef.Topic);
        if (metaDataBasketDef.Topic?.Target is { } topic)
        {
            foreach (var objects in metaDataBasketDef.Objects)
            {
                if (objects.Class is { Target: null, Path.Count: > 0 }
                    && LookupIncludingBases(topic, objects.Class.Path[0]) is ClassDef found)
                {
                    objects.Class.SetTarget(found);
                }
            }
        }

        return base.VisitMetaDataBasketDef(metaDataBasketDef);
    }

    public override bool VisitClassDef([NotNull] ClassDef classDef)
    {
        // RefHB 3.5.4-11: inside an extending topic a local name may collide with an inherited one only as an
        // explicit extension — such an EXTENDED class element extends the same-named element of the base topic
        // chain without naming it. Wire that implicit base (a synthetic target-only reference, no path); an
        // EXTENDED element without an inherited namesake keeps a null Extends and is reported by the type checker.
        if (classDef.Properties.Contains(Property.Extended) && classDef.Extends == null
            && FindInheritedNamesake<ClassDef>(classDef) is { } baseClass)
        {
            classDef.Extends = new Reference<ClassDef> { Target = baseClass, Source = classDef };
        }

        return base.VisitClassDef(classDef);
    }

    public override bool VisitAssociationDef([NotNull] AssociationDef associationDef)
    {
        // Same implicit-extension rule as for classes (RefHB 3.5.4-11).
        if (associationDef.Properties.Contains(Property.Extended) && associationDef.Extends == null
            && FindInheritedNamesake<AssociationDef>(associationDef) is { } baseAssociation)
        {
            associationDef.Extends = new Reference<AssociationDef> { Target = baseAssociation, Source = associationDef };
        }

        return base.VisitAssociationDef(associationDef);
    }

    /// <summary>
    /// The same-named element the enclosing topic inherits from its base chain, or <see langword="null"/> when the
    /// element is not inside an extending topic or no inherited namesake exists.
    /// </summary>
    private T? FindInheritedNamesake<T>(IInterlisDefinition element) where T : class, IInterlisDefinition
    {
        if (element.Parent is not TopicDef { Extends: { } topicBase })
        {
            return null;
        }

        Resolve(topicBase);
        return topicBase.Target is { } baseTopic ? LookupIncludingBases(baseTopic, element.Name) as T : null;
    }

    public override bool VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        // Register the association accesses ("Beziehungszugang", RefHB 2.7) a class gains through this role. Only a
        // role that is NOT EXTERNAL and whose target class lives in the SAME topic as the association grants an
        // access; a cross-topic / EXTERNAL role grants none, so its target class must not become navigable.
        if (attributeDef.TypeDef is RoleType roleType
            && attributeDef.Parent is AssociationDef association
            && !attributeDef.Properties.Contains(Property.External))
        {
            var associationTopic = FindRoot<TopicDef>(association);
            foreach (var target in roleType.Targets)
            {
                if (target.Value is RestrictedRef.DefinitionRef { Reference.Target: IIdentifiable identifiable }
                    && FindRoot<TopicDef>((IInterlisDefinition)identifiable) == associationTopic)
                {
                    identifiable.AssociationAccess.TryAdd(association.Name, association);
                }
            }
        }

        LinkRefSystems(attributeDef.TypeDef);
        LinkFormatAttributes(attributeDef.TypeDef);

        // A provisional UnresolvedNamedType ('attr : Foo', the merged domain/structure/class rule) can be classified now
        // that its target reference is resolved: the enclosing container's references are visited before its content
        // (see Interlis24AstBaseVisitor), so the target is already set when the attribute is visited.
        attributeDef.TypeDef = Classify(attributeDef.TypeDef);

        return base.VisitAttributeDef(attributeDef);
    }

    /// <summary>
    /// Classifies a parameter's type. A parameter is written with the same <c>attrTypeDef</c> rule as an attribute
    /// (RefHB 3.10.2), so a named type in that position is classified identically. A <c>METAOBJECT</c> parameter has
    /// no value type and is left alone.
    /// </summary>
    public override bool VisitParameterDef([NotNull] ParameterDef parameterDef)
    {
        if (parameterDef.TypeDef is { } type)
        {
            parameterDef.TypeDef = Classify(type);
        }

        return base.VisitParameterDef(parameterDef);
    }

    /// <summary>
    /// The classified form of a resolved <see cref="UnresolvedNamedType"/>: a <see cref="TypeRef"/> alias for a domain
    /// target (the same representation a domain definition uses), or a contained-substructure <see cref="ObjectType"/>
    /// for a structure / <c>ANYSTRUCTURE</c> target. Returns <paramref name="type"/> unchanged when it is not an
    /// <see cref="UnresolvedNamedType"/>, when its target is still unresolved, or for the invalid <c>ANYCLASS</c>,
    /// restricted-domain, class and association uses, which the type checker reports (RefHB 3.6.1-12).
    /// </summary>
    private static TypeDef Classify(TypeDef type)
    {
        if (type is not UnresolvedNamedType named)
        {
            return type;
        }

        TypeDef? classified = named.Target.Value switch
        {
            RestrictedRef.DefinitionRef { Reference.Target: DomainDef domain } definitionRef when named.Target.Restrictions.Count == 0
                => new TypeRef { Extends = AsDomainReference(domain, definitionRef.Reference), SourceRange = named.SourceRange },

            RestrictedRef.DefinitionRef { Reference.Target: ClassDef { IsStructure: true } } or RestrictedRef.AnyRef { Kind: RestrictedRef.AnyKind.Structure }
                => new ObjectType { Targets = [named.Target], SourceRange = named.SourceRange },

            _ => null,
        };

        if (classified == null)
        {
            return named;
        }

        classified.Cardinality = named.Cardinality;
        foreach (var (name, constraint) in named.Constraints)
        {
            classified.Constraints.TryAdd(name, constraint);
        }

        return classified;
    }

    /// <summary>
    /// Builds the resolved <see cref="DomainDef"/> reference for a domain alias from the original merged reference,
    /// preserving its path and location so go-to-definition and diagnostics keep working.
    /// </summary>
    private static Reference<DomainDef> AsDomainReference(DomainDef domain, Reference<IInterlisDefinition> original)
    {
        var reference = new Reference<DomainDef>
        {
            Source = original.Source,
            SourceRange = original.SourceRange,
            Target = domain,
        };
        reference.Path.AddRange(original.Path);
        return reference;
    }

    public override bool VisitReference<T>([NotNull] Reference<T> reference)
    {
        return Resolve(reference);
    }

    /// <summary>
    /// Assigns the final <see cref="ConstraintDef.NameIndex"/> to the constraints of a <c>CONSTRAINTS OF</c> block.
    /// The block's <see cref="ConstraintsBlockDef.Target"/> is already resolved at this point (a container's
    /// references are visited before its content), so the target viewable's constraint counter can be continued
    /// here: the numbering starts after the viewable's inline constraints and accumulates across all
    /// <c>CONSTRAINTS OF</c> blocks for the same viewable in source order, mirroring ili2c's per-viewable counter.
    /// If the target could not be resolved, the block-local index assigned during AST construction is kept.
    /// </summary>
    public override bool VisitConstraintsBlockDef([NotNull] ConstraintsBlockDef constraintsBlockDef)
    {
        if (constraintsBlockDef.Target?.Target is IConstraintContainer owner)
        {
            var index = externalConstraintIndices.GetValueOrDefault(owner, owner.Constraints.Count);
            foreach (var constraint in constraintsBlockDef.Constraints)
            {
                constraint.NameIndex = ++index;
            }

            externalConstraintIndices[owner] = index;
        }

        return base.VisitConstraintsBlockDef(constraintsBlockDef);
    }
}
