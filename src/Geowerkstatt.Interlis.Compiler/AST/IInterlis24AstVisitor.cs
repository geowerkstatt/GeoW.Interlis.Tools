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

    TResult? VisitParameterDef([NotNull] ParameterDef parameterDef);

    TResult? VisitDomainDef([NotNull] DomainDef domainDef);

    TResult? VisitUnitDef([NotNull] UnitDef unitDef);

    TResult? VisitFunctionDef([NotNull] FunctionDef functionDef);

    TResult? VisitLineFormTypeDef([NotNull] LineFormTypeDef lineFormTypeDef);

    TResult? VisitMetaDataBasketDef([NotNull] MetaDataBasketDef metaDataBasketDef);

    TResult? VisitContextDef([NotNull] ContextDef contextDef);

    TResult? VisitViewDef([NotNull] ViewDef viewDef);

    TResult? VisitBaseView([NotNull] BaseView baseView);

    TResult? VisitGraphicDef([NotNull] GraphicDef graphicDef);

    TResult? VisitMandatoryConstraint([NotNull] MandatoryConstraint mandatoryConstraint);

    TResult? VisitPlausibilityConstraint([NotNull] PlausibilityConstraint plausibilityConstraint);

    TResult? VisitExistenceConstraint([NotNull] ExistenceConstraint existenceConstraint);

    TResult? VisitUniquenessConstraint([NotNull] UniquenessConstraint uniquenessConstraint);

    TResult? VisitSetConstraint([NotNull] SetConstraint setConstraint);

    TResult? VisitConstraintsBlockDef([NotNull] ConstraintsBlockDef constraintsBlockDef);

    TResult? VisitDomainConstraint([NotNull] DomainConstraint domainConstraint);

    TResult? VisitReference<T>([NotNull] Reference<T> reference) where T : class, IReferenceTarget;
}
