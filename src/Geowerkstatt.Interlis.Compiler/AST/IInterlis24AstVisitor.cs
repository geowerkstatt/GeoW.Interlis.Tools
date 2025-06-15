using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.AST;

public interface IInterlis24AstVisitor<TResult>
{
    TResult? VisitInterlisEnvironment([NotNull] InterlisEnvironment interlisFile);

    TResult? VisitModelDef([NotNull] ModelDef modelDef);

    TResult? VisitTopicDef([NotNull] TopicDef topicDef);

    TResult? VisitClassDef([NotNull] ClassDef classDef);

    TResult? VisitAssociationDef([NotNull] AssociationDef associationDef);

    TResult? VisitAttributeDef([NotNull] AttributeDef attributeDef);

    TResult? VisitDomainDef([NotNull] DomainDef domainDef);

    TResult? VisitUnitDef([NotNull] UnitDef unitDef);

    TResult? VisitFunctionDef([NotNull] FunctionDef functionDef);

    TResult? VisitReference<T>([NotNull] Reference<T> reference) where T : class, IInterlisDefinition;
}
