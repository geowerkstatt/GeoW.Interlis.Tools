using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Tools.AST;

public class Interlis24AstBaseVisitor<TResult> : IInterlis24AstVisitor<TResult>
{
    protected internal virtual TResult? DefaultResult => default;

    public virtual TResult? VisitAssociationDef([NotNull] AssociationDef associationDef)
    {
        foreach (var element in associationDef.Content.Values)
        {
            element.Accept(this);
        }

        return DefaultResult;
    }

    public virtual TResult? VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitClassDef([NotNull] ClassDef classDef)
    {
        foreach (var element in classDef.Content.Values)
        {
            element.Accept(this);
        }

        return DefaultResult;
    }

    public virtual TResult? VisitDomainDef([NotNull] DomainDef domainDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitInterlisFile([NotNull] InterlisFile interlisFile)
    {
        foreach (var element in interlisFile.Content.Values)
        {
            element.Accept(this);
        }

        return DefaultResult;
    }

    public virtual TResult? VisitModelDef([NotNull] ModelDef modelDef)
    {
        foreach (var element in modelDef.Content.Values)
        {
            element.Accept(this);
        }

        return DefaultResult;
    }

    public virtual TResult? VisitTopicDef([NotNull] TopicDef topicDef)
    {
        foreach (var element in topicDef.Content.Values)
        {
            element.Accept(this);
        }

        return DefaultResult;
    }

    public TResult? VisitUnitDef([NotNull] UnitDef unitDef)
    {
        return DefaultResult;
    }
}
