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

    public TResult? VisitAssociationDef([NotNull] AssociationDef associationDef)
    {
        return DefaultResult;    }

    public TResult? VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        return DefaultResult;    }

    public TResult? VisitClassDef([NotNull] ClassDef classDef)
    {
        return DefaultResult;    }

    public TResult? VisitInterlisFile([NotNull] InterlisFile interlisFile)
    {
        return DefaultResult;    }

    public TResult? VisitModelDef([NotNull] ModelDef modelDef)
    {
        return DefaultResult;    }

    public TResult? VisitTopicDef([NotNull] TopicDef topicDef)
    {
        return DefaultResult;    }
}
