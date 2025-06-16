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
    /// Default implementation for visiting an <see cref="IInterlisDefinitionContainer"/>.
    /// </summary>
    private TResult? DefaultVisitIInterlisContainer([NotNull] IInterlisDefinitionContainer container)
    {
        var referenceValue = container.ContainerReferences.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
        var contentValue = container.Content.Values.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
        return AggregateResult(referenceValue, contentValue);
    }

    public virtual TResult? VisitAssociationDef([NotNull] AssociationDef associationDef)
    {
        return DefaultVisitIInterlisContainer(associationDef);
    }

    public virtual TResult? VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitClassDef([NotNull] ClassDef classDef)
    {
        return DefaultVisitIInterlisContainer(classDef);
    }

    public virtual TResult? VisitDomainDef([NotNull] DomainDef domainDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitInterlisEnvironment([NotNull] InterlisEnvironment interlisEnvironment)
    {
        return interlisEnvironment.Content.Values.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
    }

    public virtual TResult? VisitModelDef([NotNull] ModelDef modelDef)
    {
        return DefaultVisitIInterlisContainer(modelDef);
    }

    public virtual TResult? VisitTopicDef([NotNull] TopicDef topicDef)
    {
        return DefaultVisitIInterlisContainer(topicDef);
    }

    public virtual TResult? VisitUnitDef([NotNull] UnitDef unitDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitFunctionDef([NotNull] FunctionDef functionDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitReference<T>([NotNull] Reference<T> reference) where T : class, IInterlisDefinition
    {
        return DefaultResult;
    }
}
