using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public class Interlis24AstBaseVisitor<TResult> : IInterlis24AstVisitor<TResult>
{
    protected internal virtual TResult? DefaultResult => default;

    public virtual TResult? VisitAssociationDef([NotNull] AssociationDef associationDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitClassDef([NotNull] ClassDef classDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitDomainDef([NotNull] DomainDef domainDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitInterlisFile([NotNull] InterlisFile interlisFile)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitModelDef([NotNull] ModelDef modelDef)
    {
        return DefaultResult;
    }

    public virtual TResult? VisitTopicDef([NotNull] TopicDef topicDef)
    {
        return DefaultResult;
    }
}
