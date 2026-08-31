using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.AST;

public class Interlis24AstBaseVisitor<TResult> : IInterlis24AstVisitor<TResult>
{
    protected internal virtual TResult? DefaultResult => default;

    /// <summary>
    /// Aggregates the results of visiting multiple children of a node.
    /// </summary>
    /// <param name="aggregate">The previous aggregate value.</param>
    /// <param name="nextResult">The result of the immediately preceding call to visit a child node.</param>
    /// <returns>The updated aggregate result.</returns>
    protected internal virtual TResult? AggregateResult(TResult? aggregate, TResult? nextResult)
    {
        return nextResult;
    }

    /// <summary>
    /// Default implementation shared by the <c>Visit*</c> methods for every <see cref="IInterlisDefinition"/>.
    /// An <see cref="IInterlisDefinitionContainer"/> has its children traversed; any other definition has nothing
    /// to traverse and yields <see cref="DefaultResult"/>. Routing on the type keeps the container/leaf decision
    /// in one place, so a definition that later becomes a container is traversed without touching its visit method.
    /// </summary>
    private TResult? DefaultVisit([NotNull] IInterlisDefinition definition)
    {
        if (definition is not IInterlisDefinitionContainer container)
        {
            return DefaultResult;
        }

        var referenceValue = container.ContainerReferences.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
        var contentValue = container.Content.Values.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
        var result = AggregateResult(referenceValue, contentValue);

        // Constraints are held outside Content (see IConstraintContainer), so visit them here when present.
        return container is IConstraintContainer constraintContainer ? AggregateConstraints(result, constraintContainer) : result;
    }

    /// <summary>
    /// Visits the constraints of the given <paramref name="container"/>, aggregating their results onto <paramref name="aggregate"/>.
    /// </summary>
    private TResult? AggregateConstraints(TResult? aggregate, IConstraintContainer container)
    {
        return container.Constraints.Aggregate(aggregate, (accu, constraint) => AggregateResult(accu, constraint.Accept(this)));
    }

    public virtual TResult? VisitAssociationDef([NotNull] AssociationDef associationDef) => DefaultVisit(associationDef);

    public virtual TResult? VisitAttributeDef([NotNull] AttributeDef attributeDef) => DefaultVisit(attributeDef);

    public virtual TResult? VisitParameterDef([NotNull] ParameterDef parameterDef) => DefaultVisit(parameterDef);

    public virtual TResult? VisitClassDef([NotNull] ClassDef classDef) => DefaultVisit(classDef);

    public virtual TResult? VisitDomainDef([NotNull] DomainDef domainDef)
    {
        // Domain constraints live on the domain's type (RefHB 3.8-10), not in a Content/Constraints collection
        // of a container, so traverse them here.
        return domainDef.TypeDef.Constraints.Values.Aggregate(DefaultVisit(domainDef), (accu, constraint) => AggregateResult(accu, constraint.Accept(this)));
    }

    public virtual TResult? VisitModelDef([NotNull] ModelDef modelDef) => DefaultVisit(modelDef);

    public virtual TResult? VisitTopicDef([NotNull] TopicDef topicDef) => DefaultVisit(topicDef);

    public virtual TResult? VisitUnitDef([NotNull] UnitDef unitDef) => DefaultVisit(unitDef);

    public virtual TResult? VisitFunctionDef([NotNull] FunctionDef functionDef) => DefaultVisit(functionDef);

    public virtual TResult? VisitLineFormTypeDef([NotNull] LineFormTypeDef lineFormTypeDef) => DefaultVisit(lineFormTypeDef);

    public virtual TResult? VisitMetaDataBasketDef([NotNull] MetaDataBasketDef metaDataBasketDef) => DefaultVisit(metaDataBasketDef);

    public virtual TResult? VisitContextDef([NotNull] ContextDef contextDef) => DefaultVisit(contextDef);

    public virtual TResult? VisitViewDef([NotNull] ViewDef viewDef) => DefaultVisit(viewDef);

    public virtual TResult? VisitBaseView([NotNull] BaseView baseView) => DefaultVisit(baseView);

    public virtual TResult? VisitGraphicDef([NotNull] GraphicDef graphicDef) => DefaultVisit(graphicDef);

    public virtual TResult? VisitMandatoryConstraint([NotNull] MandatoryConstraint mandatoryConstraint) => DefaultVisit(mandatoryConstraint);

    public virtual TResult? VisitPlausibilityConstraint([NotNull] PlausibilityConstraint plausibilityConstraint) => DefaultVisit(plausibilityConstraint);

    public virtual TResult? VisitExistenceConstraint([NotNull] ExistenceConstraint existenceConstraint) => DefaultVisit(existenceConstraint);

    public virtual TResult? VisitUniquenessConstraint([NotNull] UniquenessConstraint uniquenessConstraint) => DefaultVisit(uniquenessConstraint);

    public virtual TResult? VisitSetConstraint([NotNull] SetConstraint setConstraint) => DefaultVisit(setConstraint);

    public virtual TResult? VisitConstraintsBlockDef([NotNull] ConstraintsBlockDef constraintsBlockDef) => DefaultVisit(constraintsBlockDef);

    public virtual TResult? VisitDomainConstraint([NotNull] DomainConstraint domainConstraint) => DefaultResult;

    public virtual TResult? VisitInterlisEnvironment([NotNull] InterlisEnvironment interlisEnvironment)
    {
        return interlisEnvironment.Content.Values.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
    }

    public virtual TResult? VisitReference<T>([NotNull] Reference<T> reference) where T : class, IReferenceTarget
    {
        return DefaultResult;
    }
}
