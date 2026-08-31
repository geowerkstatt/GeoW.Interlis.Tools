using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Performs semantic checks on the resolved AST (run after reference resolution). Reports an error for each
/// violation; the messages are diagnostic and need not match other tools verbatim.
/// </summary>
internal class Interlis24AstTypeCheckerVisitor(ILoggerFactory loggerFactory) : Interlis24AstBaseVisitor<bool>
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Interlis24AstTypeCheckerVisitor>();

    /// <summary>
    /// Effective types of domain <c>EXTENDS</c> chains computed so far, memoized for the duration of this pass so
    /// checking all domains stays linear. Values are read-only fold results (<see cref="TypeDef.MergeWithBase"/>)
    /// and never part of the AST.
    /// </summary>
    private readonly Dictionary<DomainDef, TypeDef> effectiveTypes = new();

    protected internal override bool DefaultResult => true;

    protected internal override bool AggregateResult(bool aggregate, bool nextResult) => aggregate && nextResult;

    private void ReportError(IInterlisDefinition element, string message)
    {
        logger.LogError("Type check error in '{Name}': {Message}.", element.FullyQualifiedName, message);
    }

    public override bool VisitModelDef([NotNull] ModelDef modelDef)
    {
        // The predefined INTERLIS model is trusted; do not type-check (and don't traverse) it.
        if (modelDef == InternalModel.Interlis)
        {
            return DefaultResult;
        }

        // RefHB 3.5.1-2: a TYPE model may only declare types (units, domains, functions, line forms), no topics.
        if (modelDef.Type == ModelDef.ModelType.Type)
        {
            foreach (var topic in modelDef.Content.Values.OfType<TopicDef>())
            {
                ReportError(topic, "a topic can not be part of a TYPE model");
            }
        }

        if (modelDef.TranslationOf?.Target is ModelDef originalModel)
        {
            CheckTranslationConsistency(modelDef, originalModel);
        }

        return base.VisitModelDef(modelDef);
    }

    /// <summary>
    /// RefHB 3.5.1-10: a translation may only change names, so apart from the names the model must mirror the
    /// original: the same model kind, imports of the same models, and per container the same number and kinds of
    /// elements (ili2c agrees: "The imported models do not match." / "The number of elements in ... do not
    /// match."). A differing <c>CONTRACTED</c> declaration is deliberately NOT an error (ili2c makes it one):
    /// CONTRACTED has no function anymore and is kept only for compatibility (RefHB 3.5.1-12).
    /// </summary>
    private void CheckTranslationConsistency(ModelDef model, ModelDef original)
    {
        if (model.Type != original.Type)
        {
            ReportError(model, "the model kind does not match the original model's");
        }

        CheckTranslatedImports(model, original);
        CheckTranslatedElements(model, original);
    }

    /// <summary>
    /// A translation must import the same models as the original. Two imports match when their targets lead to
    /// the same definition after following translation chains, so importing the translation of a model the
    /// original imports is allowed; the declaration order of the imports is irrelevant. Skipped while any import
    /// is unresolved — the failed resolution is already reported.
    /// </summary>
    private void CheckTranslatedImports(ModelDef model, ModelDef original)
    {
        var imports = model.Imports.Values.Select(import => import.ModelDef.Target).ToList();
        var originalImports = original.Imports.Values.Select(import => import.ModelDef.Target).ToList();
        if (imports.Contains(null) || originalImports.Contains(null))
        {
            return;
        }

        var importRoots = ImportRootsByName(imports);
        var originalImportRoots = ImportRootsByName(originalImports);
        if (!importRoots.SequenceEqual(originalImportRoots))
        {
            ReportError(model, "the imported models do not match the original model's");
        }

        static List<IInterlisDefinition> ImportRootsByName(IEnumerable<ModelDef?> imports)
            => imports
                .Select(import => TranslationRootOf(import!))
                .OrderBy(root => root.Name, StringComparer.Ordinal)
                .ToList();
    }

    /// <summary>
    /// The elements of a translated container correspond to the original's elements by declaration position
    /// (see <see cref="InterlisDefinitionContainerExtensions.InDeclarationOrder"/>), so the two containers must
    /// declare the same number of elements and each pair must be the same kind of definition, checked
    /// recursively. Checking a container stops at its first mismatch because the positional pairing beyond it
    /// is meaningless.
    /// </summary>
    private void CheckTranslatedElements(IInterlisDefinitionContainer container, IInterlisDefinitionContainer original)
    {
        if (container.Content.Count != original.Content.Count)
        {
            ReportError(container, $"the number of elements does not match the original '{((IInterlisDefinition)original).FullyQualifiedName}'");
            return;
        }

        foreach (var (element, originalElement) in container.InDeclarationOrder().Zip(original.InDeclarationOrder()))
        {
            if (element.GetType() != originalElement.GetType())
            {
                ReportError(element, $"must be the same kind of definition as the original '{originalElement.FullyQualifiedName}'");
                return;
            }

            if (element is IConstraintContainer constraintContainer && originalElement is IConstraintContainer originalConstraintContainer)
            {
                CheckTranslatedConstraints(element, constraintContainer, originalElement, originalConstraintContainer);
            }

            if (element is IInterlisDefinitionContainer childContainer && originalElement is IInterlisDefinitionContainer originalChildContainer)
            {
                CheckTranslatedElements(childContainer, originalChildContainer);
            }
        }
    }

    /// <summary>
    /// The constraints of a translated element (which are not part of <see cref="IContainer{T}.Content"/>, see
    /// <see cref="IConstraintContainer"/>) correspond to the original's constraints by position too, so there
    /// must be equally many and each pair must be the same kind of constraint.
    /// </summary>
    private void CheckTranslatedConstraints(IInterlisDefinition element, IConstraintContainer constraints, IInterlisDefinition originalElement, IConstraintContainer originalConstraints)
    {
        if (constraints.Constraints.Count != originalConstraints.Constraints.Count)
        {
            ReportError(element, $"the number of constraints does not match the original '{originalElement.FullyQualifiedName}'");
            return;
        }

        foreach (var (constraint, originalConstraint) in constraints.Constraints.Zip(originalConstraints.Constraints))
        {
            if (constraint.GetType() != originalConstraint.GetType())
            {
                ReportError(element, $"the constraint '{constraint.Name}' must be the same kind of constraint as the original's '{originalConstraint.Name}'");
                return;
            }
        }
    }

    /// <summary>
    /// RefHB 3.5.1-10: a translation may only change names — the model must otherwise mirror the original, so a
    /// translated topic must declare the same OID domains as the topic it translates. Reports an error if the
    /// OID domain declared by a translated topic differs from the original topic's. Two domains are the same
    /// when they lead to the same definition after following translation chains, so a translated OID domain
    /// matches its original. Unresolved references are skipped — the failed resolution is already reported.
    /// </summary>
    private void CheckOidDomainMatches(TopicDef topic, Reference<DomainDef>? domain, Reference<DomainDef>? originalDomain, string oidKind)
    {
        if (domain is { Target: null } || originalDomain is { Target: null })
        {
            return;
        }

        var target = domain?.Target;
        var originalTarget = originalDomain?.Target;
        if (target == originalTarget
            || (target != null && originalTarget != null && TranslationRootOf(target) == TranslationRootOf(originalTarget)))
        {
            return;
        }

        ReportError(topic, $"the {oidKind} domain does not match the original topic's");
    }

    /// <summary>
    /// Follows the translation chain of <paramref name="element"/> to the definition it translates, or returns
    /// the element itself if it is not part of a translated model. Guarded against translation cycles, which a
    /// malformed model pair can produce.
    /// </summary>
    private static IInterlisDefinition TranslationRootOf(IInterlisDefinition element)
    {
        var visited = new HashSet<IInterlisDefinition>();
        while (visited.Add(element) && element.TranslationOf?.Target is { } original)
        {
            element = original;
        }

        return element;
    }

    public override bool VisitClassDef([NotNull] ClassDef classDef)
    {
        CheckExtendsCycle(classDef, classDef.IsStructure ? "structure" : "class");
        CheckOidRedefinition(classDef);
        CheckOidAssignment(classDef, classDef.Properties, classDef.OidType, "OID");
        CheckAbstractFinal(classDef, classDef.Properties);
        CheckExtendedAndExtends(classDef, classDef.Properties, classDef.Extends);
        CheckDanglingExtended(classDef, classDef.Properties, classDef.Extends);

        var isAbstract = classDef.Properties.Contains(Property.Abstract);

        // RefHB 3.5.3-6: only an ABSTRACT class may declare abstract attributes.
        if (!isAbstract && classDef.Content.Values.OfType<AttributeDef>().Any(IsAbstractAttribute))
        {
            ReportError(classDef, "is not ABSTRACT and therefore can not have abstract attributes");
        }

        // RefHB 3.5.3: a concrete class must be part of a topic (structures may stand at model level).
        if (!isAbstract && !classDef.IsStructure && classDef.Parent is ModelDef)
        {
            ReportError(classDef, "must be declared ABSTRACT because it is not part of a topic");
        }

        if (classDef.Extends?.Target is ClassDef baseClass)
        {
            if (baseClass.Properties.Contains(Property.Final))
            {
                ReportError(classDef, $"can not extend '{((IInterlisDefinition)baseClass).FullyQualifiedName}' because it is declared FINAL");
            }

            // RefHB 3.5.3-4: a structure may be extended to a class, but a class may not be extended to a structure.
            if (classDef.IsStructure && !baseClass.IsStructure)
            {
                ReportError(classDef, "a structure can not extend a class");
            }
        }

        return base.VisitClassDef(classDef);
    }

    public override bool VisitTopicDef([NotNull] TopicDef topicDef)
    {
        CheckExtendsCycle(topicDef, "topic");
        CheckAbstractFinal(topicDef, topicDef.Properties);

        // RefHB 3.5.2-16: an inherited topic OID assignment can not be changed — refining it along the
        // replacement ladder is not a change (see CheckTopicOidRedefinition). Applies to the object and the
        // basket assignment alike; ili2c does not enforce it.
        CheckTopicOidRedefinition(topicDef, topicDef.OidType, t => t.OidType, "OID");
        CheckTopicOidRedefinition(topicDef, topicDef.BasketOidType, t => t.BasketOidType, "BASKET OID");
        CheckOidAssignment(topicDef, topicDef.Properties, topicDef.OidType, "OID");
        CheckOidAssignment(topicDef, topicDef.Properties, topicDef.BasketOidType, "BASKET OID");

        if (topicDef.TranslationOf?.Target is TopicDef originalTopic)
        {
            CheckOidDomainMatches(topicDef, topicDef.BasketOidType, originalTopic.BasketOidType, "BASKET OID");
            CheckOidDomainMatches(topicDef, topicDef.OidType, originalTopic.OidType, "OID");
        }

        if (topicDef.Extends?.Target is TopicDef baseTopic && baseTopic.Properties.Contains(Property.Final))
        {
            ReportError(topicDef, $"can not extend '{((IInterlisDefinition)baseTopic).FullyQualifiedName}' because it is declared FINAL");
        }

        // RefHB 3.5.2-13: a view topic must not contain concrete classes (tables).
        if (topicDef.IsView)
        {
            var concreteClass = topicDef.Content.Values
                .OfType<ClassDef>()
                .FirstOrDefault(c => !c.IsStructure && !c.Properties.Contains(Property.Abstract));
            if (concreteClass != null)
            {
                ReportError(topicDef, $"a view topic can not contain the concrete class '{concreteClass.Name}'");
            }
        }

        // RefHB 3.5.2-19: every DEPENDS ON must reference an existing topic.
        if (topicDef.Parent is ModelDef model)
        {
            foreach (var dependency in topicDef.DependsOn.Where(d => d.Path.Count == 1))
            {
                var name = dependency.Path[0];
                if (!(model.Content.TryGetValue(name, out var dependedOn) && dependedOn is TopicDef))
                {
                    ReportError(topicDef, $"there is no topic '{name}' to depend on");
                }
            }
        }

        // RefHB 3.5.2-20: a topic that uses a generic domain must list it under DEFERRED GENERICS.
        var deferredGenericNames = topicDef.DeferredGenerics
            .Select(generic => generic.Path.LastOrDefault())
            .WhereNotNull()
            .ToHashSet();
        foreach (var genericDomain in CollectGenericDomains(topicDef))
        {
            if (!deferredGenericNames.Contains(genericDomain.Name))
            {
                ReportError(topicDef, $"must declare DEFERRED GENERICS for the generic domain '{genericDomain.Name}'");
            }
        }

        // RefHB 3.5.2-1: a topic with an abstract definition that is NOT concretized within the same topic
        // (i.e. has no concrete extension here) must itself be declared ABSTRACT.
        if (!topicDef.Properties.Contains(Property.Abstract))
        {
            var unconcretized = topicDef.Content.Values
                .OfType<ClassDef>()
                .FirstOrDefault(c => !c.IsStructure && c.Properties.Contains(Property.Abstract) && !IsConcretizedInTopic(c, topicDef));
            if (unconcretized != null)
            {
                ReportError(topicDef, $"must be declared ABSTRACT because element '{unconcretized.Name}' is abstract");
            }
        }

        return base.VisitTopicDef(topicDef);
    }

    public override bool VisitAssociationDef([NotNull] AssociationDef associationDef)
    {
        CheckExtendsCycle(associationDef, "association");
        CheckOidRedefinition(associationDef);
        CheckOidAssignment(associationDef, associationDef.Properties, associationDef.OidType, "OID");
        CheckAbstractFinal(associationDef, associationDef.Properties);
        CheckExtendedAndExtends(associationDef, associationDef.Properties, associationDef.Extends);
        CheckDanglingExtended(associationDef, associationDef.Properties, associationDef.Extends);
        return base.VisitAssociationDef(associationDef);
    }

    /// <summary>
    /// RefHB 3.5.4-11: <c>EXTENDED</c> marks the deliberate reuse of an inherited name — without a same-named
    /// element in the enclosing topic's base chain there is nothing to extend (the resolver wires the inherited
    /// namesake when it exists). Skipped when the enclosing topic's base is unresolved (already reported).
    /// </summary>
    private void CheckDanglingExtended<T>(IInterlisDefinition element, HashSet<Property> properties, Reference<T>? extends) where T : class, IInterlisDefinition
    {
        if (properties.Contains(Property.Extended) && extends == null
            && element.Parent is not TopicDef { Extends: { Target: null } })
        {
            ReportError(element, "is marked EXTENDED but there is no inherited element of the same name");
        }
    }

    public override bool VisitViewDef([NotNull] ViewDef viewDef)
    {
        CheckExtendsCycle(viewDef, "view");
        CheckAbstractFinal(viewDef, viewDef.Properties);
        CheckExtendedAndExtends(viewDef, viewDef.Properties, viewDef.Extends);
        CheckViewBases(viewDef);
        return base.VisitViewDef(viewDef);
    }

    public override bool VisitGraphicDef([NotNull] GraphicDef graphicDef)
    {
        CheckExtendsCycle(graphicDef, "graphic");
        CheckGraphicBase(graphicDef);
        CheckDrawingRuleSignClasses(graphicDef);
        return base.VisitGraphicDef(graphicDef);
    }

    /// <summary>
    /// RefHB 3.16-1: a graphic definition is always based on a view or a class (<c>BASED ON</c>). Only a graphic that
    /// EXTENDS another one may omit the clause, because it inherits the base of the graphic it extends (RefHB 3.16-2).
    /// Being ABSTRACT does not exempt a graphic from naming its base.
    /// </summary>
    private void CheckGraphicBase(GraphicDef graphicDef)
    {
        if (graphicDef.BasedOn == null && graphicDef.Extends == null)
        {
            ReportError(graphicDef, "must be BASED ON a class or view, or EXTEND a graphic to inherit its base");
        }
    }

    /// <summary>
    /// RefHB 3.16-4: as soon as a drawing rule is concrete, the class of the graphic signatures it assigns must be
    /// defined. A rule states that class with its own <c>OF Sign-ClassRef</c>; an <c>(EXTENDED)</c> rule may omit it
    /// and keeps the sign class of the rule it refines. An <c>(ABSTRACT)</c> rule is not concrete and is therefore
    /// exempt — note ili2c is stricter here and demands <c>OF</c> on an abstract rule too.
    /// </summary>
    private void CheckDrawingRuleSignClasses(GraphicDef graphicDef)
    {
        foreach (var drawingRule in graphicDef.DrawingRules)
        {
            if (drawingRule.Sign != null
                || drawingRule.Properties.Contains(Property.Abstract)
                || drawingRule.Properties.Contains(Property.Extended))
            {
                continue;
            }

            ReportError(graphicDef, $"the drawing rule '{drawingRule.Name}' must specify the class of the graphic signatures it assigns ('OF ...')");
        }
    }

    public override bool VisitMandatoryConstraint([NotNull] MandatoryConstraint mandatoryConstraint)
    {
        CheckConstraintCondition(mandatoryConstraint, mandatoryConstraint.Condition);
        return base.VisitMandatoryConstraint(mandatoryConstraint);
    }

    public override bool VisitPlausibilityConstraint([NotNull] PlausibilityConstraint plausibilityConstraint)
    {
        CheckConstraintCondition(plausibilityConstraint, plausibilityConstraint.Condition);
        return base.VisitPlausibilityConstraint(plausibilityConstraint);
    }

    public override bool VisitSetConstraint([NotNull] SetConstraint setConstraint)
    {
        CheckConstraintCondition(setConstraint, setConstraint.Condition);
        if (setConstraint.Where != null)
        {
            CheckConstraintCondition(setConstraint, setConstraint.Where);
        }

        return base.VisitSetConstraint(setConstraint);
    }

    /// <summary>
    /// RefHB 3.12: a constraint condition (and a <c>SET CONSTRAINT</c> <c>WHERE</c> pre-condition) must be a logical
    /// (boolean) expression. Only expressions whose value type is known here can be judged: a comparison / logical /
    /// <c>NOT</c> / <c>DEFINED</c> expression yields a boolean, whereas a numeric, text or enumeration value clearly
    /// does not. A path (<see cref="ObjectType"/>), an unresolved reference (<see cref="UndefinedType"/>) or an alias
    /// of an unresolved domain (a <see cref="TypeRef"/> that could not be seen through) carries no resolved value
    /// type at this stage and is therefore left unchecked to avoid false positives.
    /// </summary>
    private void CheckConstraintCondition(ConstraintDef constraint, IExpression condition)
    {
        if (condition.ReturnType is not BooleanType and not ObjectType and not UndefinedType and not TypeRef)
        {
            ReportError(constraint, "the constraint condition must be a boolean expression");
        }
    }

    /// <summary>
    /// Resolves and checks the base names ("Basissichten") of a view and the object paths formulated over them
    /// (RefHB 3.15). Only formation views are analysed; a view that only <c>EXTENDS</c> another inherits its
    /// bases and is out of scope for this pass.
    /// </summary>
    private void CheckViewBases(ViewDef viewDef)
    {
        if (viewDef.Formation == null)
        {
            // A view that only EXTENDS another inherits its bases; resolving those is out of scope for this pass.
            return;
        }

        // The view's base names come from the base views the AST builder registered in Content — the namespace where
        // base names live (Bestandteilnamen, RefHB 3.5.4), alongside attributes and roles. We validate ALL OF and
        // object-path heads against them.
        var baseNames = viewDef.Content.Values.OfType<BaseView>().Select(baseView => baseView.Name).ToHashSet();

        // RefHB 3.15: a base viewable that lives in another topic requires a topic dependency.
        CheckBaseTopicDependencies(viewDef);

        // Resolve/check every object path formulated in the view: its head must denote a base of the view (RefHB
        // 3.13/3.15). Unlike a class, a view has no implicit "this object", so an unqualified attribute name or a
        // reference to something outside the view is not a valid path head.
        foreach (var path in CollectViewPaths(viewDef))
        {
            CheckPathHead(viewDef, path, baseNames);
        }
    }

    private void CheckBaseTopicDependencies(ViewDef viewDef)
    {
        var sourceTopic = FindTopic(viewDef);
        if (sourceTopic == null)
        {
            return;
        }

        foreach (var baseView in viewDef.Content.Values.OfType<BaseView>())
        {
            if (baseView.Viewable?.Target is not { } target)
            {
                continue;
            }

            var targetTopic = FindTopic(target);
            if (targetTopic != null && targetTopic != sourceTopic
                && !sourceTopic.DependsOn.Any(dependency => dependency.Path.LastOrDefault() == targetTopic.Name))
            {
                ReportError(viewDef, $"the base viewable '{target.Name}' is in topic '{targetTopic.Name}' and requires a topic dependency");
            }
        }
    }

    /// <summary>
    /// The object paths formulated inside a view: the selections (<c>WHERE</c>) and the defining factors of its
    /// derived attributes. Their heads are resolved against the view's bases (see <see cref="CheckPathHead"/>).
    /// </summary>
    private static IEnumerable<PathExpression> CollectViewPaths(ViewDef viewDef)
    {
        foreach (var selection in viewDef.Selections)
        {
            foreach (var path in CollectPaths(selection))
            {
                yield return path;
            }
        }

        foreach (var attribute in viewDef.Content.Values.OfType<AttributeDef>())
        {
            foreach (var value in attribute.Values)
            {
                foreach (var path in CollectPaths(value))
                {
                    yield return path;
                }
            }
        }
    }

    /// <summary>Recursively collects the <see cref="PathExpression"/>s contained in an expression tree.</summary>
    private static IEnumerable<PathExpression> CollectPaths(IExpression expression)
    {
        switch (expression)
        {
            case PathExpression path:
                yield return path;
                break;
            case BinaryExpression binary:
                foreach (var path in CollectPaths(binary.FirstOperand)) yield return path;
                foreach (var path in CollectPaths(binary.SecondOperand)) yield return path;
                break;
            case UnaryExpression unary:
                foreach (var path in CollectPaths(unary.Operand)) yield return path;
                break;
            case FunctionCall call:
                foreach (var argument in call.Arguments)
                {
                    foreach (var path in CollectPaths(argument)) yield return path;
                }
                break;
            case InspectionExpression { Of: { } ofPath }:
                // The OF restriction path of an inspection factor is rooted like any other path (RefHB 3.13-48);
                // the inline inspection itself carries no PathExpression.
                yield return ofPath;
                break;
        }
    }

    private void CheckPathHead(ViewDef viewDef, PathExpression path, IReadOnlySet<string> baseNames)
    {
        // A path that starts with a keyword (THIS, AGGREGATES, ...) or is empty does not address a base by name;
        // a role or indexed-attribute head carries a name that would have to denote a base — it never can, since
        // bases are plain names — so it is reported like any other non-base head.
        var head = path.Path.FirstOrDefault() switch
        {
            IdentifierPathElement identifier => identifier.Value,
            RolePathElement role => role.Name,
            AttributePathElement indexed => indexed.Name,
            _ => null,
        };

        if (head != null && !baseNames.Contains(head))
        {
            ReportError(viewDef, $"the path must start with a base of the view, but '{head}' is not a base");
        }
    }

    public override bool VisitFunctionDef([NotNull] FunctionDef functionDef)
    {
        // RefHB 3.14-12: an OBJECT/OBJECTS argument takes a RestrictedClassOrAssRef — a class, an association or
        // ANYCLASS. ANYSTRUCTURE is not among its alternatives (ili2c's grammar rejects it outright); the merged
        // restricted-reference rule accepts it for uniform parsing, so it is enforced here.
        foreach (var argument in functionDef.Arguments)
        {
            if (argument.Type is ObjectType { Targets: [{ Value: RestrictedRef.AnyRef { Kind: RestrictedRef.AnyKind.Structure } }] })
            {
                ReportError(functionDef, $"the object argument '{argument.Name}' can not take ANYSTRUCTURE; a class, an association or ANYCLASS is required");
            }
        }

        if (functionDef.ReturnType is ObjectType { Targets: [{ Value: RestrictedRef.AnyRef { Kind: RestrictedRef.AnyKind.Structure } }] })
        {
            ReportError(functionDef, "the object result can not take ANYSTRUCTURE; a class, an association or ANYCLASS is required");
        }

        return base.VisitFunctionDef(functionDef);
    }

    public override bool VisitUnitDef([NotNull] UnitDef unitDef)
    {
        CheckExtendsCycle(unitDef, "unit");

        // RefHB 3.9.1: only an abstract unit may be extended. A concrete unit is fully defined and therefore
        // can not serve as the base of another unit.
        if (unitDef.Extends?.Target is { } baseUnit && !baseUnit.Properties.Contains(Property.Abstract))
        {
            ReportError(unitDef, $"can not extend '{((IInterlisDefinition)baseUnit).FullyQualifiedName}' because it is not ABSTRACT");
        }

        return base.VisitUnitDef(unitDef);
    }

    public override bool VisitMetaDataBasketDef([NotNull] MetaDataBasketDef metaDataBasketDef)
    {
        CheckExtendsCycle(metaDataBasketDef, "basket");
        return base.VisitMetaDataBasketDef(metaDataBasketDef);
    }

    public override bool VisitLineFormTypeDef([NotNull] LineFormTypeDef lineFormTypeDef)
    {
        // RefHB 3.8.12.3-4: the structure describing the geometry of a curve segment must always be an
        // extension of the predefined structure INTERLIS.LineSegment (which carries the segment end point every
        // curve form shares). ili2c does not enforce the rule — it accepts any structure, even a class — we
        // reject. An unresolved structure reference is already reported by the resolver.
        if (lineFormTypeDef.Structure?.Target is { } structure && !ExtendsLineSegment(structure))
        {
            ReportError(lineFormTypeDef, $"the line structure '{structure.Name}' must be an extension of the predefined structure INTERLIS.LineSegment");
        }

        return base.VisitLineFormTypeDef(lineFormTypeDef);
    }

    /// <summary>
    /// Whether the structure transitively extends the predefined <c>INTERLIS.LineSegment</c> structure — itself
    /// excluded: RefHB 3.8.12.3-4 demands an extension, and the abstract predefined structure describes no
    /// concrete curve form. Guarded against extension cycles, which are not diagnosed anywhere yet.
    /// </summary>
    private static bool ExtendsLineSegment(ClassDef structure)
    {
        var lineSegment = InternalModel.Interlis.Content["LineSegment"];
        var visited = new HashSet<ClassDef>();
        for (var current = structure.Extends?.Target; current != null && visited.Add(current); current = current.Extends?.Target)
        {
            if (current == lineSegment)
            {
                return true;
            }
        }

        return false;
    }

    public override bool VisitDomainDef([NotNull] DomainDef domainDef)
    {
        CheckDomainExtension(domainDef);

        // A primary enumeration definition (extensions are validated by CheckDomainExtension against the
        // inherited enumeration) must obey the standalone enumeration rules: unique element names per nesting
        // (RefHB 3.8.2-5) and no dotted element names (RefHB 3.8.2-17).
        if (domainDef.TypeDef is EnumerationType { Extends: null } enumeration)
        {
            CheckEnumerationDefinition(domainDef, enumeration);
        }

        // RefHB 3.8-3: an incomplete value-range definition must be declared ABSTRACT (a coordinate range may
        // alternatively be GENERIC, RefHB 3.8.8-16). Judged on the EFFECTIVE type: definition parts omitted in an
        // extension are inherited from the base chain (RefHB 3.8-4), e.g. a line extension that only adds the
        // direction (`DirectedLine EXTENDS Line = DIRECTED POLYLINE;`) is complete through its base.
        if (!domainDef.Properties.Contains(Property.Abstract) && !domainDef.Properties.Contains(Property.Generic)
            && IsIncompleteType(EffectiveTypeOf(domainDef)))
        {
            ReportError(domainDef, "must be declared ABSTRACT because its type is not fully defined");
        }

        // RefHB 3.8.5-6: an abstract unit is only allowed while the value range is still undefined (NUMERIC).
        if (domainDef.TypeDef is (DecimalType or FloatType) and NumericType { Unit.Target: { } declaredUnit }
            && declaredUnit.Properties.Contains(Property.Abstract))
        {
            ReportError(domainDef, $"the abstract unit '{declaredUnit.Name}' is only allowed while the value range is undefined");
        }

        CheckFormattedRangeHasFormat(domainDef, domainDef.TypeDef);
        CheckRefSystems(domainDef, domainDef.TypeDef);
        CheckDomainConstraints(domainDef);

        return base.VisitDomainDef(domainDef);
    }

    /// <summary>
    /// The rules for a domain's value restrictions (<c>CONSTRAINTS</c>, RefHB 3.8-8/-10): each condition is a
    /// <c>Logical-Expression</c>, so it must be boolean (like a class constraint's — an unresolved or
    /// object-valued result stays unchecked to avoid false positives, which also keeps a bare <c>THIS</c> legal),
    /// and a name may not repeat one from the inherited chain — every restriction of the chain applies
    /// (RefHB 3.8-8 "gelten alle", no override semantics), so a reused name would leave two same-named
    /// restrictions in the effective domain, unaddressable for diagnostics and tooling. The inherited names are
    /// collected by walking the authored base chain (the fold deliberately does not accumulate constraints), so
    /// a constraint-only intermediate domain contributes its names too. Per-definition name uniqueness
    /// (RefHB 3.8-8) is enforced by construction: <see cref="TypeDef.Constraints"/> is keyed by name and the
    /// build visitor reports duplicates. ili2c enforces none of this (probed 2026-08-29: <c>CONSTRAINTS c: 5</c>,
    /// duplicate and inherited-name-reusing constraints are all accepted) — RefHB-strict divergences.
    /// </summary>
    private void CheckDomainConstraints(DomainDef domainDef)
    {
        foreach (var constraint in domainDef.TypeDef.Constraints.Values)
        {
            if (constraint.Condition.ReturnType is not BooleanType and not ObjectType and not UndefinedType and not TypeRef)
            {
                ReportError(domainDef, $"the condition of domain constraint '{constraint.Name}' must be a boolean expression");
            }
        }

        if (domainDef.TypeDef.Constraints.Count > 0)
        {
            var inherited = new HashSet<string>();
            var visited = new HashSet<DomainDef>();
            for (var baseDomain = domainDef.TypeDef.Extends?.Target; baseDomain != null && visited.Add(baseDomain); baseDomain = baseDomain.TypeDef.Extends?.Target)
            {
                inherited.UnionWith(baseDomain.TypeDef.Constraints.Keys);
            }

            foreach (var name in domainDef.TypeDef.Constraints.Keys.Where(inherited.Contains))
            {
                ReportError(domainDef, $"the domain constraint name '{name}' is already used by an inherited constraint");
            }
        }
    }

    /// <summary>
    /// Validates the reference-system links of a numeric type or the axes of a coordinate type (RefHB 3.8.5-19):
    /// the meta object of a basket-qualified <c>{basket.metaObject}</c> form must be declared by that basket or
    /// one it extends (RefHB 3.10.1-2/-3). The <c>&lt;...&gt;</c> form needs no check here — its reference is
    /// typed to <see cref="DomainDef"/>, so a non-domain target stays unresolved and is reported by the resolver.
    /// An unresolved basket is already reported by the resolver; an unqualified meta-object name is not validated
    /// (which basket provides it is a runtime concern, RefHB 3.10.1-3), and whether the meta object's class kind
    /// fits the usage (scalar vs coordinate system) is not checked yet.
    /// </summary>
    private void CheckRefSystems(IInterlisDefinition element, TypeDef type)
    {
        switch (type)
        {
            case NumericType { RefSystem: { } refSystem }:
                CheckRefSystem(element, refSystem);
                break;

            case CoordType coord:
                foreach (var axis in coord.Axis)
                {
                    if (axis.RefSystem is { } axisRefSystem)
                    {
                        CheckRefSystem(element, axisRefSystem);
                    }
                }

                break;
        }
    }

    private void CheckRefSystem(IInterlisDefinition element, RefSys refSystem)
    {
        if (refSystem.Value is RefSys.MetaObjectRef { Basket.Target: { } basket, MetaObject: { Target: null, Path: [{ } metaObjectName] } })
        {
            ReportError(element, $"the basket '{basket.Name}' does not declare the meta object '{metaObjectName}'");
        }
    }

    /// <summary>
    /// RefHB 3.8.6-3: the bare <c>Min..Max</c> form of a formatted range carries no format of its own — it
    /// restricts an inherited formatted domain. Without a <c>FORMAT</c> definition, a formatted base domain or an
    /// <c>EXTENDS</c> base there is no format to interpret the bounds by (ili2c rejects the form too).
    /// </summary>
    private void CheckFormattedRangeHasFormat(IInterlisDefinition element, TypeDef type)
    {
        if (type is FormattedType { Format: null, BasedOn: null, FormatBaseType: null, Min: not null, Extends: null })
        {
            ReportError(element, "a formatted range without a format definition must extend a formatted domain");
        }
    }

    /// <summary>
    /// Whether the type definition is incomplete and therefore requires its carrier to be declared ABSTRACT
    /// (domains: RefHB 3.8-3, attributes: RefHB 3.6.1-1): a bound-less <c>NUMERIC</c> (RefHB 3.8.5-1), a
    /// coordinate type with missing or bound-less axes (RefHB 3.8.8-16), a line type missing its line form or
    /// vertex declaration or using an abstract vertex domain (RefHB 3.8.12.2-1/-26), or an alias of a domain
    /// declared ABSTRACT. A GENERIC domain is usable at concrete use sites (RefHB 3.8-3/3.8.8-18). PARAMETERs are
    /// never checked — the predefined <c>AXIS</c> structure itself (RefHB 3.10.3-4, not abstract) declares a
    /// bound-less <c>NUMERIC</c> parameter. Unresolved references are not judged.
    /// <para>
    /// Domains are judged on their EFFECTIVE type (parts omitted in an extension are inherited, RefHB 3.8-4);
    /// attributes through <see cref="IsIncompleteAttributeType"/>, which judges an extended attribute's inline
    /// type on its effective type for the same reason and any other attribute type as authored, where the
    /// <see cref="TypeRef"/> case covers a domain reference — an ABSTRACT domain is only usable in ABSTRACT
    /// attributes regardless of how complete its type is (RefHB 3.8.8-17).
    /// </para>
    /// </summary>
    private static bool IsIncompleteType(TypeDef type) => type switch
    {
        NumericType numeric => numeric.GetType() == typeof(NumericType),
        // An OID value range is as complete as its inner type: OID NUMERIC leaves the range undefined like any
        // bound-less NUMERIC (RefHB 3.8.5-1). The OID ANY and NOOID states are not incomplete definitions — the
        // predefined NOOID itself is declared without ABSTRACT (RefHB 3.8.9-6); the ANYOID assignment rule is
        // enforced separately (CheckOidAssignment, RefHB 3.8.9-14).
        OidType { Value: OidType.ValueRange { Type: { } inner } } => IsIncompleteType(inner),
        CoordType coord => coord.Axis.Count == 0 || coord.Axis.Exists(IsIncompleteType),
        ILineType line => line.LineForms.Count == 0
            || line.VertexType is not { } vertex
            || (vertex.Target is { } vertexDomain && IsAbstractDomain(vertexDomain)),
        TypeRef { Extends.Target: { } domain } => IsAbstractDomain(domain),
        _ => false,
    };

    /// <summary>Whether uses of the domain are incomplete: declared ABSTRACT and not GENERIC (RefHB 3.8.8-18).</summary>
    private static bool IsAbstractDomain(DomainDef domain) =>
        domain.Properties.Contains(Property.Abstract) && !domain.Properties.Contains(Property.Generic);

    /// <summary>
    /// The effective type of the domain's whole <c>EXTENDS</c> chain: the root's type with every extension's
    /// declared parts merged on top, root-first (<see cref="TypeDef.MergeWithBase"/>). This — not the immediate
    /// base's declared type — is what an extension must be checked against: a base may omit definition parts
    /// (bounds, precision, unit, text length) and thereby INHERIT them from its own ancestors (RefHB 3.8-4), so
    /// comparing against the declared type alone would let an extension silently widen past a bounded ancestor.
    /// Cyclic chains are folded best-effort from an arbitrary cut (the cycle itself is reported separately).
    /// </summary>
    private TypeDef EffectiveTypeOf(DomainDef domain)
    {
        if (effectiveTypes.TryGetValue(domain, out var known))
        {
            return known;
        }

        var chain = new List<DomainDef> { domain };
        TypeDef? effective = null;
        var visited = new HashSet<DomainDef> { domain };
        for (var current = domain.TypeDef.Extends?.Target; current != null && visited.Add(current); current = current.TypeDef.Extends?.Target)
        {
            if (effectiveTypes.TryGetValue(current, out effective))
            {
                break;
            }

            chain.Add(current);
        }

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var authored = chain[i].TypeDef;
            effective = effective == null ? authored : authored.MergeWithBase(effective);
            effectiveTypes[chain[i]] = effective;
        }

        return effective!;
    }

    /// <summary>
    /// The effective type a use-site type node stands for: its own declared parts merged on top of the effective
    /// type of the domain chain it extends (a pure alias contributes nothing); a node extending no domain is its
    /// own effective type.
    /// </summary>
    private TypeDef EffectiveTypeOf(TypeDef type) =>
        type.Extends?.Target is { } baseDomain ? type.MergeWithBase(EffectiveTypeOf(baseDomain)) : type;

    /// <summary>
    /// RefHB 3.8.1: an extended domain must restrict its base. A circular <c>EXTENDS</c> chain restricts nothing and
    /// makes the domain unusable, and a domain of a different kind than its inherited type — or one changing the
    /// numeric precision (RefHB 3.8.5-4) or widening the inherited value range / text length — is not a
    /// restriction. Each check compares against the effective type of the base chain
    /// (<see cref="EffectiveTypeOf(DomainDef)"/>), so parts a base merely inherits are enforced too. A pure alias
    /// (<see cref="TypeRef"/>, no local type) restricts nothing locally and is always legal; an unresolved base or
    /// a broken local type is left unchecked.
    /// </summary>
    private void CheckDomainExtension(DomainDef domainDef)
    {
        if (IsInExtendsCycle(domainDef))
        {
            ReportError(domainDef, "the domain transitively EXTENDS itself");
            return;
        }

        var type = domainDef.TypeDef;
        if (type is TypeRef or UndefinedType || type.Extends?.Target is not { } baseDomain)
        {
            return;
        }

        var baseType = EffectiveTypeOf(baseDomain);
        if (baseType is TypeRef or UndefinedType)
        {
            return;
        }

        // A bound-less NUMERIC (a plain NumericType, no notation subtype) declares neither bounds nor notation,
        // so it does not clash with either notation as a kind (a base is concretized in either notation; an
        // extension REMOVING the bounds is rejected below with the more precise message); every other extension
        // must keep its base's kind.
        var openNumericInvolved = type is NumericType && baseType is NumericType
            && (type.GetType() == typeof(NumericType) || baseType.GetType() == typeof(NumericType));
        if (type.GetType() != baseType.GetType() && !openNumericInvolved)
        {
            ReportError(domainDef, $"the domain must be of the same kind as its base '{baseDomain.Name}'");
            return;
        }

        if (type is NumericType numeric && baseType is NumericType baseNumeric)
        {
            // A NUMERIC without bounds counts as ABSTRACT (RefHB 3.8.5-1). Concretizing an abstract base is the
            // normal direction of an extension; the reverse — a bound-less NUMERIC over an inherited concrete
            // range — abstracts the definition again instead of restricting it (RefHB 3.8-4) and is rejected, as
            // ili2c does ("Abstract numeric types can not extend concrete numeric types"). In the RefHB 3.8.5-13
            // example every legal bound-less extension sits on a bound-less base; the only bound-less domain over
            // a concrete range is the line marked invalid. (The effective type still inherits the bounds, so
            // extensions of such an illegal middle domain are still checked against the inherited range.)
            if (numeric.GetType() == typeof(NumericType) && baseNumeric is DecimalType or FloatType)
            {
                ReportError(domainDef, "an abstract NUMERIC can not extend a concrete numeric range");
            }

            // RefHB 3.8.5-4: the precision (Stellenzahl) may not be changed in an extension — in either direction
            // (the RefHB 3.8.5-5 example marks the refined 'Genau' as invalid). A changed precision also puts the
            // bounds on a different grid, so the range comparison is only meaningful when the precisions match.
            // A notation change (DecimalType vs FloatType) is already rejected as a kind change above.
            else if (IsPrecisionChanged(numeric, baseNumeric))
            {
                ReportError(domainDef, "the precision must match the inherited precision");
            }
            else if (IsRangeWider(numeric, baseNumeric))
            {
                // RefHB 3.8.5-4: the bounds may only be narrowed.
                ReportError(domainDef, "the value range must not be wider than the inherited range");
            }

            CheckNumericUnitExtension(domainDef, numeric, baseNumeric);
        }

        if (type is TextType text && baseType is TextType baseText)
        {
            // RefHB 3.8.1-3: an extended text length must not exceed the inherited length — and a bare
            // TEXT/MTEXT declares an UNLIMITED length, so it can not extend a length-restricted base either.
            // MTEXT (which permits line breaks) is wider than TEXT.
            if (baseText.Length is { } baseLength && (text.Length is not { } length || length > baseLength))
            {
                ReportError(domainDef, "the text length must not exceed the inherited length");
            }

            if (text.IsMText && !baseText.IsMText)
            {
                ReportError(domainDef, "an MTEXT can not extend a TEXT");
            }
        }

        if (type is EnumerationType enumeration && baseType is EnumerationType baseEnumeration)
        {
            CheckEnumerationExtension(domainDef, enumeration, baseEnumeration);
        }

        if (type is OidType oidType && baseType is OidType baseOidType)
        {
            // RefHB 3.8.9-13: an OID definition can not be extended, except that NOOID (the NoOid root) may be
            // extended by ANYOID and an OID ANY definition by a concrete one ("nicht OID ANY"). A concrete OID
            // definition is therefore final: narrowing an identity value range would invalidate identities
            // existing objects already carry. ili2c does not enforce the rule. A parse-broken value (null) is
            // left unchecked.
            if (baseOidType.Value is OidType.ValueRange)
            {
                ReportError(domainDef, "a concrete OID definition can not be extended");
            }
            else if (baseOidType.Value is OidType.AnyOid && oidType.Value is OidType.AnyOid)
            {
                ReportError(domainDef, "an OID ANY definition can only be extended by a concrete OID definition (not OID ANY)");
            }
        }
    }

    /// <summary>
    /// RefHB 3.8.2-17/-18/-19: validates an enumeration extension's delta tree — the authored tree on its own
    /// (<see cref="CheckAuthoredEnumeration"/>), then the merge against the inherited (effective) enumeration.
    /// An authored element naming an inherited element must refine it with a sub-enumeration — turning an
    /// inherited leaf into a node or extending the node's sub-enumeration — so re-listing one without a
    /// sub-enumeration re-defines it; any other element is an addition, which an inherited enumeration marked
    /// FINAL forbids at that nesting; a dotted name must identify an inherited element (ili2c accepts unmatched
    /// dotted names as new nested elements, we reject per the RefHB 3.8.2-17 wording); and a CIRCULAR
    /// enumeration can not be extended at all — not even by only refining leaves (RefHB 3.8.2-20 makes no
    /// exception; ili2c accepts circular extensions entirely).
    /// </summary>
    private void CheckEnumerationExtension(IInterlisDefinition element, EnumerationType extension, EnumerationType baseEnumeration)
    {
        if (baseEnumeration.Sequencing == EnumerationType.Sequencings.Circular)
        {
            ReportError(element, "a CIRCULAR enumeration can not be extended");
        }

        CheckAuthoredEnumeration(element, extension.Values, isExtension: true, path: string.Empty);
        var effective = EnumerationType.CopyTree(baseEnumeration.Values);
        EnumerationType.MergeEnumeration(effective, extension.Values, new HashSet<EnumerationTreeNode>(), string.Empty, message => ReportError(element, message));
    }

    /// <summary>
    /// Validates a primary enumeration definition: the standalone authored-tree rules
    /// (<see cref="CheckAuthoredEnumeration"/>), with nothing inherited to merge against.
    /// </summary>
    private void CheckEnumerationDefinition(IInterlisDefinition element, EnumerationType enumeration)
    {
        CheckAuthoredEnumeration(element, enumeration.Values, isExtension: false, path: string.Empty);
    }

    /// <summary>
    /// Validates an authored enumeration tree independently of any inherited enumeration: element names must be
    /// unique within each nesting (RefHB 3.8.2-5) — compared by their full dotted names, so the deltas
    /// <c>rot.a (...)</c> and <c>rot.b (...)</c> coexist while listing <c>gelb</c> twice is a duplicate even
    /// when each occurrence legally refines an inherited element (ili2c accepts that, we reject) — and a dotted
    /// element name is only allowed in an extension (RefHB 3.8.2-17) and must define the sub-enumeration it
    /// refines the identified element with. Diagnostics name elements by their full path in enumeration-constant
    /// form (<c>#rot.a</c>).
    /// </summary>
    private void CheckAuthoredEnumeration(IInterlisDefinition element, EnumerationValuesList values, bool isExtension, string path)
    {
        var names = new HashSet<string>();
        foreach (var node in values)
        {
            // Collect the element's full dotted name: a flagged single sub-element continues the name, anything
            // else is the element's defined sub-enumeration.
            var end = node;
            var name = node.Name;
            while (end.SubValues.Count == 1 && end.SubValues[0].FromDottedName)
            {
                end = end.SubValues[0];
                name = $"{name}.{end.Name}";
            }

            var fullPath = path.Length == 0 ? name : $"{path}.{name}";
            if (!names.Add(name))
            {
                ReportError(element, $"duplicate enumeration element '#{fullPath}'");
            }

            if (end.FromDottedName && !isExtension)
            {
                ReportError(element, $"the dotted element name '#{fullPath}' is only allowed in an extension of an enumeration");
            }
            else if (end.FromDottedName && end.SubValues.Count == 0 && !end.SubValues.IsFinal)
            {
                ReportError(element, $"the dotted element name '#{fullPath}' must define a sub-enumeration");
            }

            // A bare (FINAL) freezes an existing sub-enumeration of an extended enumeration (RefHB 3.8.2-19);
            // outside an extension there is nothing it could freeze — it only creates an empty FINAL
            // sub-enumeration whose element is no valid value (only leaves are values, RefHB 3.8.2-1). In an
            // extension the merge reports the same mistake against the inherited tree.
            if (!isExtension && end.SubValues.Count == 0 && end.SubValues.IsFinal)
            {
                ReportError(element, $"the element '#{fullPath}' has no sub-enumeration to declare FINAL");
            }

            CheckAuthoredEnumeration(element, end.SubValues, isExtension, fullPath);
        }
    }

    /// <summary>
    /// RefHB 3.8.5-8/-9/-10: a numeric extension may not introduce a unit over a concrete unit-less base, must
    /// refine an inherited ABSTRACT unit with an extension of that unit, and can not override an inherited
    /// CONCRETE unit. A concrete range that keeps an inherited abstract unit without concretizing it is also
    /// rejected (RefHB 3.8.5-6: abstract units are only allowed while the value range is undefined). Compared
    /// against the EFFECTIVE base type, so units inherited across intermediate domains are enforced too. An
    /// unresolved unit reference on either side is left unchecked.
    /// </summary>
    private void CheckNumericUnitExtension(IInterlisDefinition element, NumericType type, NumericType baseType)
    {
        var baseUnit = baseType.Unit?.Target;
        if (type.Unit?.Target is { } unit && unit != baseUnit)
        {
            if (baseUnit == null)
            {
                if (baseType is DecimalType or FloatType)
                {
                    ReportError(element, "the inherited definition has no unit, so the extension can not introduce one");
                }
            }
            else if (baseUnit.Properties.Contains(Property.Abstract))
            {
                if (!IsOrExtendsUnit(unit, baseUnit))
                {
                    ReportError(element, $"the unit '{unit.Name}' must be an extension of the inherited abstract unit '{baseUnit.Name}'");
                }
            }
            else
            {
                ReportError(element, $"the inherited concrete unit '{baseUnit.Name}' can not be overridden");
            }
        }
        else if (type.Unit == null && type is DecimalType or FloatType
            && baseUnit is { } inherited && inherited.Properties.Contains(Property.Abstract))
        {
            ReportError(element, $"the inherited abstract unit '{inherited.Name}' must be concretized along with the value range");
        }
    }

    /// <summary>
    /// Whether the unit is the base unit or a transitive extension of it. Every unit inherits <c>ANYUNIT</c>
    /// implicitly, without declaring it (RefHB 3.9.1-4). A derived unit (defined by a conversion expression
    /// instead of <c>EXTENDS</c>) is automatically an extension of the same abstract unit as the single unit it is
    /// derived from (RefHB 3.9.2-7); composed units referencing several units are not traversed (matching them
    /// against composed abstract units would need a structural comparison).
    /// </summary>
    private static bool IsOrExtendsUnit(UnitDef unit, UnitDef baseUnit)
    {
        if (baseUnit == InternalModel.Interlis.Content["ANYUNIT"])
        {
            return true;
        }

        var visited = new HashSet<UnitDef>();
        for (var current = unit; current != null && visited.Add(current);)
        {
            if (current == baseUnit)
            {
                return true;
            }

            current = current.Extends?.Target ?? SingleDerivationBase(current);
        }

        return false;
    }

    /// <summary>
    /// The single unit a derived unit's conversion expression refers to, or <see langword="null"/> for a unit
    /// that is not derived or is composed of several units.
    /// </summary>
    private static UnitDef? SingleDerivationBase(UnitDef unit)
    {
        var referenced = ReferencedUnits(unit.Expression).Distinct().ToList();
        return referenced.Count == 1 ? referenced[0] : null;
    }

    /// <summary>All units referenced within a unit conversion expression.</summary>
    private static IEnumerable<UnitDef> ReferencedUnits(IExpression? expression) => expression switch
    {
        UnitReferenceExpression { Unit.Target: { } unit } => [unit],
        BinaryExpression binary => ReferencedUnits(binary.FirstOperand).Concat(ReferencedUnits(binary.SecondOperand)),
        _ => [],
    };

    /// <summary>
    /// Whether the extension changes the precision (Stellenzahl) of its numeric base (RefHB 3.8.5-4): the step of a
    /// decimal range, the mantissa length of a float range — and a notation change, which replaces the base's grid
    /// altogether. Bound-less <c>NUMERIC</c> bases or extensions (plain <see cref="NumericType"/>) are not compared.
    /// </summary>
    private static bool IsPrecisionChanged(NumericType type, NumericType baseType) => (type, baseType) switch
    {
        (DecimalType extension, DecimalType baseDecimal) => extension.Precision != baseDecimal.Precision,
        (FloatType extension, FloatType baseFloat) => extension.MantissaLength != baseFloat.MantissaLength,
        (DecimalType, FloatType) or (FloatType, DecimalType) => true,
        _ => false,
    };

    /// <summary>
    /// Whether the extending numeric range is wider than the inherited one. Callers only compare ranges whose
    /// precisions match (RefHB 3.8.5-4), so the bounds lie on the same grid and are compared exactly. Open
    /// (unbounded) ranges on either side are not compared.
    /// </summary>
    private static bool IsRangeWider(NumericType type, NumericType baseType) =>
        type.Min is { } min && type.Max is { } max
        && baseType.Min is { } baseMin && baseType.Max is { } baseMax
        && (min < baseMin || max > baseMax);

    /// <summary>
    /// Reports a definition whose <c>EXTENDS</c> chain leads back to itself: the definition is transitively its
    /// own base, so it extends nothing and every walk along its base chain would loop. Each definition inside
    /// the cycle is reported once; a definition merely extending INTO a foreign cycle is not (its own chain
    /// never returns to it). ili2c rejects extension cycles too — mostly through its single-pass name
    /// resolution, which can not even resolve the forward reference a cycle needs.
    /// </summary>
    private void CheckExtendsCycle<T>(T definition, string kind) where T : class, IInterlisDefinition, IExtending<T>
    {
        var visited = new HashSet<T>();
        for (var current = definition.Extends?.Target; current != null && visited.Add(current); current = current.Extends?.Target)
        {
            if (current == definition)
            {
                ReportError(definition, $"the {kind} transitively EXTENDS itself");
                return;
            }
        }
    }

    /// <summary>
    /// RefHB 3.5.3-2: an inherited OID definition can only be replaced along the state ladder — "ein geerbtes
    /// NO OID durch ANY, und ein geerbtes ANY durch eine konkrete Definition" — never backwards. The ladder is
    /// judged on the states (the <see cref="OidType.Target"/> of the effective types), not on the domains'
    /// extension chains: any "konkrete Definition" may replace an inherited ANY, whether or not its domain
    /// extends the inherited one (domain extensions are policed separately by the RefHB 3.8.9-13 rule).
    /// Repeating a definition, or replacing one with a definition in the same state, is no replacement. An
    /// unresolved or non-OID side is left unchecked (the kind check reports the latter); ili2c does not enforce
    /// the rule at all.
    /// </summary>
    private void CheckOidRedefinition<T>(T definition) where T : class, IInterlisDefinition, IIdentifiable, IExtending<T>
    {
        if (definition.OidType?.Target is { } newDomain && InheritedOidDomain(definition) is { } inherited)
        {
            ReportOidRedefinition(definition, "OID", newDomain, inherited);
        }
    }

    /// <summary>
    /// Reports a replacement that leaves the RefHB 3.5.3-2 state ladder (see <see cref="CheckOidRedefinition{T}"/>):
    /// a concrete inherited definition is final — RefHB 3.8.9-13 makes it inextensible, so no compliant
    /// replacement exists — and an inherited open definition can not fall back to <c>NO OID</c> ("Ein geerbtes
    /// ANY kann jedoch nicht durch NO OID ersetzt werden").
    /// </summary>
    private void ReportOidRedefinition(IInterlisDefinition element, string kind, DomainDef newDomain, DomainDef inherited)
    {
        switch (EffectiveTypeOf(inherited))
        {
            case OidType { Value: OidType.ValueRange } when newDomain != inherited:
                ReportError(element, $"the inherited {kind} definition '{inherited.Name}' is concrete and can not be changed");
                break;

            case OidType { Value: OidType.AnyOid } when EffectiveTypeOf(newDomain) is OidType { Value: OidType.NoOid }:
                ReportError(element, $"the inherited {kind} definition '{inherited.Name}' can not be replaced by NO OID");
                break;
        }
    }

    /// <summary>
    /// Validates an <c>OID AS</c> / <c>BASKET OID AS</c> assignment, judged on the referenced domain's effective
    /// type (so a domain inheriting its OID type through an <c>EXTENDS</c> chain qualifies; an unresolved
    /// reference is left unchecked): the domain must be an OID value range (<c>OID ...</c>, RefHB 3.8.9-1/-3 —
    /// the one rule ili2c reports too, "Domain ... should be an OIDType"), and an <c>ANYOID</c> assignment —
    /// identifications expected while "die genaue Definition aber noch offen ist" — demands an ABSTRACT carrier
    /// (RefHB 3.8.9-14: otherwise ANYOID is only usable as an attribute value range; ili2c is lenient). The
    /// NOOID root needs no abstractness: it declares the identification unstable, not undecided.
    /// </summary>
    private void CheckOidAssignment(IInterlisDefinition element, HashSet<Property> properties, Reference<DomainDef>? oid, string kind)
    {
        switch (oid?.Target is { } domain ? EffectiveTypeOf(domain) : null)
        {
            case null or TypeRef or UndefinedType:
                break;

            case OidType { Value: OidType.AnyOid } when !properties.Contains(Property.Abstract):
                ReportError(element, $"must be declared ABSTRACT because its {kind} definition '{oid.Target!.Name}' is still open");
                break;

            case OidType:
                break;

            default:
                ReportError(element, $"the {kind} definition '{oid.Target!.Name}' must be an OID domain");
                break;
        }
    }

    /// <summary>
    /// The OID domain the definition inherits: the nearest <c>OID AS</c> / <c>NO OID</c> definition along its
    /// <c>EXTENDS</c> chain (the topic default is not part of the chain — it applies only where the whole chain
    /// is silent and constrains nothing, RefHB 3.5.3-2). Cycle-guarded; an unresolved definition yields
    /// <see langword="null"/> (unchecked).
    /// </summary>
    private static DomainDef? InheritedOidDomain<T>(T definition) where T : class, IInterlisDefinition, IIdentifiable, IExtending<T>
    {
        var visited = new HashSet<T>();
        for (var current = definition.Extends?.Target; current != null && visited.Add(current); current = current.Extends?.Target)
        {
            if (current.OidType is { } oid)
            {
                return oid.Target;
            }
        }

        return null;
    }

    /// <summary>
    /// RefHB 3.5.3-2: <c>NO OID</c> declares the class's object identification unstable, "als Folge ist es
    /// weder möglich, Objekte dieser Klasse inkrementell nachzuliefern, noch Referenzen (Beziehungen,
    /// Beziehungsattribute) auf diese Klasse zu definieren" — so such a class can not be a reference-attribute
    /// or role target. The class's effective definition is the nearest one along its <c>EXTENDS</c> chain,
    /// falling back to the enclosing topic's inherited assignment ("Fehlt die Definition, gilt diejenige des
    /// Themas"). The final fallback — "Fehlt die Definition auch beim Thema, gilt implizit NO OID", which by the
    /// letter would make such classes unreferenceable too — is deliberately NOT flagged: the published ecosystem
    /// (172 repository models at the time of writing, the federal Geometry/Graphic base models among them)
    /// defines associations in topics without any OID declaration, as do the RefHB's own examples. An unresolved
    /// definition counts as stable (unchecked). ili2c does not enforce the rule at all.
    /// </summary>
    private bool LacksStableOid(ClassDef classDef)
    {
        var visited = new HashSet<ClassDef>();
        for (var current = classDef; current != null && visited.Add(current); current = current.Extends?.Target)
        {
            if (current.OidType is { } oid)
            {
                return IsNoOid(oid);
            }
        }

        if (FindTopic(classDef) is { } topic && (topic.OidType ?? InheritedTopicOid(topic, t => t.OidType)) is { } topicOid)
        {
            return IsNoOid(topicOid);
        }

        return false;
    }

    /// <summary>Whether the OID assignment resolves to the unstable <c>NOOID</c> state (unresolved counts as stable).</summary>
    private bool IsNoOid(Reference<DomainDef> oid) =>
        oid.Target is { } domain && EffectiveTypeOf(domain) is OidType { Value: OidType.NoOid };

    /// <summary>
    /// Reports a topic extension that changes an inherited OID assignment: RefHB 3.5.2-16 forbids changing it,
    /// read in harmony with the RefHB 3.5.3-2 state ladder (see <see cref="CheckOidRedefinition{T}"/>) — the
    /// nearest assignment of the same kind along the topic's <c>EXTENDS</c> chain may be repeated or moved up
    /// the ladder (<c>ANYOID</c> concretized), everything else is a change. The literal no-change reading would
    /// leave an inherited <c>ANYOID</c> assignment — explicitly "die genaue Definition aber noch offen"
    /// (RefHB 3.8.9-14) — permanently unclosable for the <c>BASKET OID</c>, which has no class-level escape
    /// hatch. An unresolved side stays unchecked.
    /// </summary>
    private void CheckTopicOidRedefinition(TopicDef topicDef, Reference<DomainDef>? own, Func<TopicDef, Reference<DomainDef>?> slot, string kind)
    {
        if (own?.Target is { } newDomain && InheritedTopicOid(topicDef, slot)?.Target is { } inherited)
        {
            ReportOidRedefinition(topicDef, kind, newDomain, inherited);
        }
    }

    /// <summary>
    /// The nearest OID assignment of the given slot along the topic's <c>EXTENDS</c> chain, the topic itself
    /// excluded. Cycle-guarded; the nearest assignment wins even when its reference is unresolved.
    /// </summary>
    private static Reference<DomainDef>? InheritedTopicOid(TopicDef topicDef, Func<TopicDef, Reference<DomainDef>?> slot)
    {
        var visited = new HashSet<TopicDef>();
        for (var current = topicDef.Extends?.Target; current != null && visited.Add(current); current = current.Extends?.Target)
        {
            if (slot(current) is { } inherited)
            {
                return inherited;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the domain's <c>EXTENDS</c> chain leads back to the domain itself. Guarded against cycles it is not
    /// part of (e.g. a chain that runs into a cycle between two other domains).
    /// </summary>
    private static bool IsInExtendsCycle(DomainDef domainDef)
    {
        var visited = new HashSet<DomainDef>();
        for (var current = domainDef.TypeDef.Extends?.Target; current != null && visited.Add(current); current = current.TypeDef.Extends?.Target)
        {
            if (current == domainDef)
            {
                return true;
            }
        }

        return false;
    }

    public override bool VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        // RefHB 3.6.3: validate proper reference attributes (REFERENCE TO); a ReferenceType is only ever a REFERENCE TO.
        if (attributeDef.TypeDef is ReferenceType reference && reference.Target.Value is RestrictedRef.DefinitionRef { Reference.Target: { } target })
        {
            CheckReferenceAttribute(attributeDef, reference, target);
        }

        // RefHB 3.7.4: an association role targeting a class in another topic requires a topic dependency.
        if (attributeDef.TypeDef is RoleType roleType)
        {
            CheckRoleAttribute(attributeDef, roleType);
        }

        // RefHB 3.6.1-13/-15/-17: the merged grammar rule for restricted references permits ANYCLASS,
        // ANYSTRUCTURE and RESTRICTION in every context; enforce which of them each context actually allows.
        CheckRestrictedRefContext(attributeDef);

        // RefHB 3.6.1-1: an attribute whose type is abstract must itself be declared ABSTRACT.
        if (!attributeDef.Properties.Contains(Property.Abstract) && IsIncompleteAttributeType(attributeDef))
        {
            ReportError(attributeDef, "must be declared ABSTRACT because its type is not fully defined");
        }

        // RefHB 3.6.1-5: TRANSIENT excludes an attribute from the transfer because its value is fixed by a factor
        // and only meaningful inside other factors — without a factor assignment a transient attribute would have
        // no value at all.
        if (attributeDef.Properties.Contains(Property.Transient) && attributeDef.Values.Count == 0)
        {
            ReportError(attributeDef, "is TRANSIENT but has no factor assignment fixing its value");
        }

        CheckFormattedRangeHasFormat(attributeDef, attributeDef.TypeDef);
        CheckRefSystems(attributeDef, attributeDef.TypeDef);

        // RefHB 3.6.1/3.6.4: an extended attribute may only narrow its base attribute.
        if (attributeDef.Properties.Contains(Property.Extended) && FindBaseAttribute(attributeDef) is { } baseAttribute)
        {
            CheckAttributeNarrowing(attributeDef, baseAttribute);
        }

        // An inline enumeration that refines no inherited attribute is a primary definition and must obey the
        // standalone enumeration rules (unique element names, no dotted names); the EXTENDED case above validates
        // the delta against the inherited enumeration instead.
        else if (attributeDef.TypeDef is EnumerationType enumeration)
        {
            CheckEnumerationDefinition(attributeDef, enumeration);
        }

        return base.VisitAttributeDef(attributeDef);
    }

    private void CheckAttributeNarrowing(AttributeDef attribute, AttributeDef baseAttribute)
    {
        var ordered = attribute.TypeDef.Cardinality?.Ordered ?? false;
        var baseOrdered = baseAttribute.TypeDef.Cardinality?.Ordered ?? false;

        // An extension may not drop the inherited order promise: RefHB 3.6.4-5 for substructures (a LIST may
        // not be extended by a BAG) and the same rule for the ordered link set of a role (RefHB 3.7.4; ili2c:
        // "An unordered role can not extend an ordered one").
        if (baseOrdered && !ordered)
        {
            ReportError(attribute, attribute.TypeDef is RoleType
                ? "an unordered role can not extend an ordered one"
                : "a BAG (unordered) can not extend a LIST (ordered)");
        }

        // RefHB 3.6.1-6: an extension may only restrict, so the cardinality must lie within the inherited one —
        // a MANDATORY base can not become optional and a collection can not grow.
        if (attribute.TypeDef.Cardinality is { } cardinality && baseAttribute.TypeDef.Cardinality is { } baseCardinality
            && ((cardinality.Min ?? 0) < (baseCardinality.Min ?? 0)
                || (cardinality.Max ?? long.MaxValue) > (baseCardinality.Max ?? long.MaxValue)))
        {
            ReportError(attribute, "the cardinality must not be wider than the inherited cardinality");
        }

        // Compare the effective types, so a domain alias or a type inheriting parts of its EXTENDS chain is
        // checked by what it stands for, not by its declared node alone.
        if (EffectiveTypeOf(attribute.TypeDef) is NumericType numeric && EffectiveTypeOf(baseAttribute.TypeDef) is NumericType baseNumeric)
        {
            // A NUMERIC without bounds counts as abstract (RefHB 3.8.5-1) and abstracts the inherited concrete
            // range again instead of restricting it — same rule as for domain extensions.
            if (numeric.GetType() == typeof(NumericType) && baseNumeric is DecimalType or FloatType)
            {
                ReportError(attribute, "an abstract NUMERIC can not extend a concrete numeric range");
            }

            // RefHB 3.6.1/3.8.5-4: the precision may not be changed in an extension; the range comparison is only
            // meaningful when the precisions match (same grid).
            else if (IsPrecisionChanged(numeric, baseNumeric))
            {
                ReportError(attribute, "the precision must match the inherited precision");
            }
            else if (IsRangeWider(numeric, baseNumeric))
            {
                ReportError(attribute, "the value range must not be wider than the inherited range");
            }

            CheckNumericUnitExtension(attribute, numeric, baseNumeric);
        }

        // RefHB 3.8.2-18/-19: an inline enumeration on an extended attribute is a delta on the inherited
        // enumeration, validated like an enumeration domain extension — against the base attribute's effective
        // enumeration, so deltas inherited across intermediate extended attributes are enforced too.
        if (attribute.TypeDef is EnumerationType enumeration && EffectiveTypeOf(baseAttribute) is EnumerationType baseEnumeration)
        {
            CheckEnumerationExtension(attribute, enumeration, baseEnumeration);
        }
    }

    /// <summary>
    /// The effective type of an attribute, folding attribute-level EXTENDED redefinitions: an extended
    /// attribute's inline type inherits the definition parts it omits from the base attribute's effective type,
    /// like a domain extension does from its base (RefHB 3.8-4) — an enumeration delta tree merges into the
    /// inherited tree (RefHB 3.8.2-18/-19), a line type inherits the line form and vertex declaration
    /// (RefHB 3.8.12.2-23), and kinds without inheritable parts replace the inherited type
    /// (<see cref="TypeDef.MergeWithBase"/>). A type referencing a domain replaces the inherited type with that
    /// domain's (folded) type instead — the reference is the complete definition, not a delta. Guarded against
    /// class extension cycles, which would loop the base-attribute walk forever.
    /// </summary>
    private TypeDef EffectiveTypeOf(AttributeDef attribute, HashSet<AttributeDef>? visited = null)
    {
        if (attribute.Properties.Contains(Property.Extended)
            && attribute.TypeDef.Extends == null
            && (visited ??= new HashSet<AttributeDef>()).Add(attribute)
            && FindBaseAttribute(attribute) is { } baseAttribute)
        {
            return attribute.TypeDef.MergeWithBase(EffectiveTypeOf(baseAttribute, visited));
        }

        return EffectiveTypeOf(attribute.TypeDef);
    }

    private static AttributeDef? FindBaseAttribute(AttributeDef attribute) => attribute.Parent switch
    {
        ClassDef classDef => FindBaseAttribute(classDef, attribute.Name),
        AssociationDef associationDef => FindBaseAttribute(associationDef, attribute.Name),
        _ => null,
    };

    /// <summary>
    /// The nearest attribute (or role) of the given name inherited through the container's <c>EXTENDS</c> chain.
    /// Guarded against an extension cycle (reported by <see cref="CheckExtendsCycle"/>), which would otherwise
    /// loop this walk forever.
    /// </summary>
    private static AttributeDef? FindBaseAttribute<T>(T container, string name) where T : class, IInterlisDefinitionContainer, IExtending<T>
    {
        var visited = new HashSet<T>();
        for (var baseContainer = container.Extends?.Target; baseContainer != null && visited.Add(baseContainer); baseContainer = baseContainer.Extends?.Target)
        {
            if (baseContainer.Content.TryGetValue(name, out var element) && element is AttributeDef baseAttribute)
            {
                return baseAttribute;
            }
        }

        return null;
    }

    private void CheckRoleAttribute(AttributeDef role, RoleType roleType)
    {
        // RefHB 3.5.3-2: a class without stable object identification can not be referenced. Structure targets
        // are not judged here — a structure can not carry an OID at all and is rejected as a role target on its
        // own account.
        foreach (var target in roleType.Targets)
        {
            if (target.Value is RestrictedRef.DefinitionRef { Reference.Target: ClassDef { IsStructure: false } noOidTarget }
                && LacksStableOid(noOidTarget))
            {
                ReportError(role, $"can not reference '{noOidTarget.Name}' because it has no stable object identification (NO OID)");
            }
        }

        var sourceTopic = FindTopic(role);
        if (sourceTopic == null)
        {
            return;
        }

        // A role whose target class lives in another topic requires that topic to be a declared dependency.
        foreach (var target in roleType.Targets)
        {
            if (target.Value is not RestrictedRef.DefinitionRef { Reference.Target: ClassDef targetClass })
            {
                continue;
            }

            var targetTopic = FindTopic(targetClass);
            if (targetTopic != null && targetTopic != sourceTopic
                && !sourceTopic.DependsOn.Any(dependency => dependency.Path.LastOrDefault() == targetTopic.Name))
            {
                ReportError(role, $"the cross-topic role requires a topic dependency on '{targetTopic.Name}'");
            }
        }
    }

    private void CheckReferenceAttribute(AttributeDef attribute, ReferenceType reference, IInterlisDefinition target)
    {
        // A reference attribute may only reference a class (not a structure or an association).
        if (target is not ClassDef targetClass || targetClass.IsStructure)
        {
            ReportError(attribute, "a reference attribute may only reference a class");
            return;
        }

        // RefHB 3.5.3-2: a class without stable object identification can not be referenced.
        if (LacksStableOid(targetClass))
        {
            ReportError(attribute, $"can not reference '{targetClass.Name}' because it has no stable object identification (NO OID)");
        }

        // Extension attributes inherit their restrictions / EXTERNAL from the base; only check fresh ones.
        if (attribute.Properties.Contains(Property.Extended))
        {
            return;
        }

        // Each RESTRICTION must be the referenced class itself or an extension of it.
        foreach (var restriction in reference.Target.Restrictions)
        {
            if (restriction.Target is ClassDef restrictionClass && restrictionClass != targetClass && !ExtendsTransitively(restrictionClass, targetClass))
            {
                ReportError(attribute, $"RESTRICTION '{restrictionClass.Name}' must be an extension of '{targetClass.Name}'");
            }
        }

        // A reference to a class in another topic requires EXTERNAL, which in turn requires a topic dependency.
        // A class declared in a BASE topic of the referencing topic is not external: the extending topic inherits
        // the base topic's classes (RefHB 3.5.4-11), so the reference stays within the topic.
        var sourceTopic = FindTopic(attribute);
        var targetTopic = FindTopic(targetClass);
        if (sourceTopic != null && targetTopic != null && sourceTopic != targetTopic && !ExtendsTransitively(sourceTopic, targetTopic))
        {
            if (!reference.Properties.Contains(Property.External))
            {
                ReportError(attribute, "a cross-topic reference requires property EXTERNAL");
            }
            else if (!sourceTopic.DependsOn.Any(dependency => dependency.Path.LastOrDefault() == targetTopic.Name))
            {
                ReportError(attribute, $"the EXTERNAL reference requires a topic dependency on '{targetTopic.Name}'");
            }
        }
    }

    /// <summary>
    /// Validates the <c>ANYCLASS</c> / <c>ANYSTRUCTURE</c> / <c>RESTRICTION</c> use of a restricted reference against
    /// the context it appears in. The grammar collapses <c>DomainRef</c>, <c>RestrictedStructureRef</c> and
    /// <c>RestrictedClassOrAssRef</c> into one rule that permits all three everywhere, so the per-context
    /// constraints (RefHB 3.6.1-13/-15/-17) are re-imposed here rather than in the grammar.
    /// </summary>
    private void CheckRestrictedRefContext(AttributeDef attributeDef)
    {
        switch (attributeDef.TypeDef)
        {
            // Attribute type = DomainRef | RestrictedStructureRef: ANYSTRUCTURE is allowed but ANYCLASS is not, and a
            // RESTRICTION only narrows a structure — a plain domain reference can never be restricted (RefHB 3.6.1-13/-17, 3.8-12).
            // Only these invalid uses are still an UnresolvedNamedType here: a valid domain or structure reference has
            // already been rewritten to a TypeRef / contained-substructure ObjectType by the reference resolver.
            case UnresolvedNamedType namedType:
                if (namedType.Target.Value is RestrictedRef.AnyRef { Kind: RestrictedRef.AnyKind.Class })
                {
                    ReportError(attributeDef, "ANYCLASS is not allowed as an attribute type");
                }

                if (namedType.Target.Restrictions.Count > 0 && namedType.Target.Value is RestrictedRef.DefinitionRef { Reference.Target: DomainDef })
                {
                    ReportError(attributeDef, "a domain reference can not be restricted");
                }

                // A class is not a value domain: its objects are only reachable through a reference attribute (RefHB 3.6.3).
                if (namedType.Target.Value is RestrictedRef.DefinitionRef { Reference.Target: ClassDef { IsStructure: false } })
                {
                    ReportError(attributeDef, "a class can only be referenced with REFERENCE TO");
                }

                // An association is not a value domain either (RefHB 3.6.1-13).
                if (namedType.Target.Value is RestrictedRef.DefinitionRef { Reference.Target: AssociationDef })
                {
                    ReportError(attributeDef, "an association is not allowed as an attribute type");
                }

                break;

            // Reference attribute (REFERENCE TO) = RestrictedClassOrAssRef: ANYCLASS is allowed but ANYSTRUCTURE is not (RefHB 3.6.1-15).
            case ReferenceType reference:
                if (reference.Target.Value is RestrictedRef.AnyRef { Kind: RestrictedRef.AnyKind.Structure })
                {
                    ReportError(attributeDef, "ANYSTRUCTURE is not allowed as a reference target");
                }

                break;

            // Association role target = RestrictedClassOrAssRef: ANYSTRUCTURE is not allowed (RefHB 3.7.1).
            case RoleType roleType:
                if (roleType.Targets.Any(target => target.Value is RestrictedRef.AnyRef { Kind: RestrictedRef.AnyKind.Structure }))
                {
                    ReportError(attributeDef, "ANYSTRUCTURE is not allowed as an association role target");
                }

                break;
        }
    }

    private static TopicDef? FindTopic(IInterlisDefinition element)
    {
        for (var current = element.Parent; current != null; current = current.Parent)
        {
            if (current is TopicDef topic)
            {
                return topic;
            }
        }

        return null;
    }

    /// <summary>
    /// The distinct generic domains (<c>(GENERIC)</c>) referenced by the attributes of the classes in a topic.
    /// </summary>
    private static IEnumerable<DomainDef> CollectGenericDomains(TopicDef topic) =>
        topic.Content.Values
            .OfType<ClassDef>()
            .SelectMany(classDef => classDef.Content.Values.OfType<AttributeDef>())
            .Select(attribute => (attribute.TypeDef as TypeRef)?.Extends?.Target)
            .WhereNotNull()
            .Where(domain => domain.Properties.Contains(Property.Generic))
            .Distinct();

    /// <summary>
    /// Whether the abstract <paramref name="abstractClass"/> is concretized within <paramref name="topic"/>,
    /// i.e. some concrete class in the topic extends it (directly or transitively).
    /// </summary>
    private static bool IsConcretizedInTopic(ClassDef abstractClass, TopicDef topic) =>
        topic.Content.Values.OfType<ClassDef>()
            .Any(candidate => !candidate.Properties.Contains(Property.Abstract) && ExtendsTransitively(candidate, abstractClass));

    private static bool ExtendsTransitively<T>(T definition, T target) where T : class, IInterlisDefinition, IExtending<T>
    {
        // Guarded against an extension cycle (e.g. CLASS A EXTENDS A), which is not diagnosed anywhere yet and
        // would otherwise loop this walk forever.
        var visited = new HashSet<T>();
        for (var current = definition.Extends?.Target; current != null && visited.Add(current); current = current.Extends?.Target)
        {
            if (current == target)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether an attribute is abstract: either declared <c>ABSTRACT</c> or having an abstract (incomplete) type.
    /// </summary>
    private bool IsAbstractAttribute(AttributeDef attribute) =>
        attribute.Properties.Contains(Property.Abstract) || IsIncompleteAttributeType(attribute);

    /// <summary>
    /// Whether the attribute's type is incomplete (<see cref="IsIncompleteType"/>): an extended attribute's
    /// inline type is judged on its effective type — parts it omits are inherited from the base attribute
    /// (<see cref="EffectiveTypeOf(AttributeDef, HashSet{AttributeDef})"/>), e.g. an extended line attribute
    /// adding only the direction is complete through its base. A type referencing a domain stays judged on the
    /// authored alias: an ABSTRACT domain is only usable in ABSTRACT attributes regardless of how complete its
    /// type is (RefHB 3.8.8-17).
    /// </summary>
    private bool IsIncompleteAttributeType(AttributeDef attribute) =>
        attribute.Properties.Contains(Property.Extended) && attribute.TypeDef.Extends == null
            ? IsIncompleteType(EffectiveTypeOf(attribute))
            : IsIncompleteType(attribute.TypeDef);

    /// <summary>
    /// RefHB 3.4: a construct can not be declared as both <c>ABSTRACT</c> and <c>FINAL</c>.
    /// </summary>
    private void CheckAbstractFinal(IInterlisDefinition element, HashSet<Property> properties)
    {
        if (properties.Contains(Property.Abstract) && properties.Contains(Property.Final))
        {
            ReportError(element, "can not be both ABSTRACT and FINAL");
        }
    }

    /// <summary>
    /// RefHB 3.4: <c>EXTENDED</c> and <c>EXTENDS</c> can not be used together. Only an AUTHORED extends clause
    /// counts (non-empty path): for an EXTENDED element the resolver wires the inherited namesake as a synthetic,
    /// path-less <see cref="Reference{T}"/>, which is the meaning of EXTENDED rather than a conflict with it.
    /// </summary>
    private void CheckExtendedAndExtends<T>(IInterlisDefinition element, HashSet<Property> properties, Reference<T>? extends) where T : class, IInterlisDefinition
    {
        if (properties.Contains(Property.Extended) && extends is { Path.Count: > 0 })
        {
            ReportError(element, "can not use both EXTENDED and EXTENDS");
        }
    }
}
