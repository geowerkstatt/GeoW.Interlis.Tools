using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.AST;

public class Interlis24AstBaseVisitor<TResult> : IInterlis24AstVisitor<TResult>
{
    protected internal virtual TResult? DefaultResult => default;

    /// <summary>
    /// Aggregates the results of visiting multiple children of a node.
    /// </summary>
    /// <param name="aggregate">The previous aggregate value.</param>
    /// <param name="nextResult">The result of the immediately preceeding call to visit a child node.</param>
    /// <returns>The updated aggregate result.</returns>
    protected internal virtual TResult? AggregateResult(TResult? aggregate, TResult? nextResult)
    {
        return nextResult;
    }

    public virtual TResult? VisitAssociationDef([NotNull] AssociationDef associationDef)
    {
        return associationDef.Content.Values.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
    }

    public virtual TResult? VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitClassDef([NotNull] ClassDef classDef)
    {
        return classDef.Content.Values.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
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
        return modelDef.Content.Values.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
    }

    public virtual TResult? VisitTopicDef([NotNull] TopicDef topicDef)
    {
        return topicDef.Content.Values.Aggregate(DefaultResult, (accu, element) => AggregateResult(accu, element.Accept(this)));
    }

    public virtual TResult? VisitUnitDef([NotNull] UnitDef unitDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitFunctionDef([NotNull] FunctionDef functionDef)
    {
        return DefaultResult;
    }
}
