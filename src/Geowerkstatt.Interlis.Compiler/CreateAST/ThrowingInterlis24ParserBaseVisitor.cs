using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace Geowerkstatt.Interlis.Tools.CreateAST;

/// <summary>
/// Base visitor that throws a <see cref="NotImplementedException"/> for all rule visit methods.
/// </summary>
public class ThrowingInterlis24ParserBaseVisitor<TResult> : AbstractParseTreeVisitor<TResult>, IInterlis24ParserVisitor<TResult>
{
    /// <summary>
    /// Throws a <see cref="NotImplementedException"/> with some additional information.
    /// </summary>
    private TResult ThrowNotImplementedException(ParserRuleContext context)
    {
        var name = Interlis24Parser.ruleNames[context.RuleIndex];
        var token = context.Start;
        throw new NotImplementedException($"Rule '{name}' at line {token.Line}:{token.Column} not implemented.");
    }

    public virtual TResult VisitAggregation([NotNull] Interlis24Parser.AggregationContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAlignmentType([NotNull] Interlis24Parser.AlignmentTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitArgument([NotNull] Interlis24Parser.ArgumentContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitArgumentType([NotNull] Interlis24Parser.ArgumentTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAssociationDef([NotNull] Interlis24Parser.AssociationDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAssociationPath([NotNull] Interlis24Parser.AssociationPathContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAttributeDef([NotNull] Interlis24Parser.AttributeDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAttributePathConst([NotNull] Interlis24Parser.AttributePathConstContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAttributePathType([NotNull] Interlis24Parser.AttributePathTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAttributeRef([NotNull] Interlis24Parser.AttributeRefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAttrType([NotNull] Interlis24Parser.AttrTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitAttrTypeDef([NotNull] Interlis24Parser.AttrTypeDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitBaseAttrRef([NotNull] Interlis24Parser.BaseAttrRefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitBaseExtensionDef([NotNull] Interlis24Parser.BaseExtensionDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitBaseType([NotNull] Interlis24Parser.BaseTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitBlackboxType([NotNull] Interlis24Parser.BlackboxTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitBooleanType([NotNull] Interlis24Parser.BooleanTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitCardinality([NotNull] Interlis24Parser.CardinalityContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitClassConst([NotNull] Interlis24Parser.ClassConstContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitClassDef([NotNull] Interlis24Parser.ClassDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitClassContent([NotNull] Interlis24Parser.ClassContentContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitClassType([NotNull] Interlis24Parser.ClassTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitComposedUnit([NotNull] Interlis24Parser.ComposedUnitContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitCondSignParamAssignment([NotNull] Interlis24Parser.CondSignParamAssignmentContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitConstant([NotNull] Interlis24Parser.ConstantContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitConstraintDef([NotNull] Interlis24Parser.ConstraintDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitConstraintsDef([NotNull] Interlis24Parser.ConstraintsDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitContextDef([NotNull] Interlis24Parser.ContextDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitControlPoints([NotNull] Interlis24Parser.ControlPointsContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitCoordinateType([NotNull] Interlis24Parser.CoordinateTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitDateTimeType([NotNull] Interlis24Parser.DateTimeTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitDecConst([NotNull] Interlis24Parser.DecConstContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitDefinitionRef([NotNull] Interlis24Parser.DefinitionRefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitDerivedUnit([NotNull] Interlis24Parser.DerivedUnitContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitDomainDef([NotNull] Interlis24Parser.DomainDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitDrawingRule([NotNull] Interlis24Parser.DrawingRuleContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitEnumAssignment([NotNull] Interlis24Parser.EnumAssignmentContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitEnumElement([NotNull] Interlis24Parser.EnumElementContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitEnumeration([NotNull] Interlis24Parser.EnumerationContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitEnumerationConst([NotNull] Interlis24Parser.EnumerationConstContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitEnumerationType([NotNull] Interlis24Parser.EnumerationTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitEnumRange([NotNull] Interlis24Parser.EnumRangeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitEnumTreeValueType([NotNull] Interlis24Parser.EnumTreeValueTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitExistenceConstraint([NotNull] Interlis24Parser.ExistenceConstraintContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitExpression([NotNull] Interlis24Parser.ExpressionContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitFactor([NotNull] Interlis24Parser.FactorContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitFormatDef([NotNull] Interlis24Parser.FormatDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitFormationDef([NotNull] Interlis24Parser.FormationDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitFormattedType([NotNull] Interlis24Parser.FormattedTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitFunctionCall([NotNull] Interlis24Parser.FunctionCallContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitFunctionDef([NotNull] Interlis24Parser.FunctionDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitGlobalUniqueness([NotNull] Interlis24Parser.GlobalUniquenessContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitGraphicDef([NotNull] Interlis24Parser.GraphicDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitInspection([NotNull] Interlis24Parser.InspectionContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitInterlis([NotNull] Interlis24Parser.InterlisContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitIntersectionDef([NotNull] Interlis24Parser.IntersectionDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitJoin([NotNull] Interlis24Parser.JoinContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitLineForm([NotNull] Interlis24Parser.LineFormContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitLineFormType([NotNull] Interlis24Parser.LineFormTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitLineFormTypeDef([NotNull] Interlis24Parser.LineFormTypeDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitLineType([NotNull] Interlis24Parser.LineTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitLocalUniqueness([NotNull] Interlis24Parser.LocalUniquenessContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitMandatoryConstraint([NotNull] Interlis24Parser.MandatoryConstraintContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitMetaAttribute([NotNull] Interlis24Parser.MetaAttributeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitMetaAttributes([NotNull] Interlis24Parser.MetaAttributesContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitMetaDataBasketDef([NotNull] Interlis24Parser.MetaDataBasketDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitMetaObjectRef([NotNull] Interlis24Parser.MetaObjectRefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitModelContents([NotNull] Interlis24Parser.ModelContentsContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitModelDef([NotNull] Interlis24Parser.ModelDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitNumericConst([NotNull] Interlis24Parser.NumericConstContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitNumericType([NotNull] Interlis24Parser.NumericTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitObjectOrAttributePath([NotNull] Interlis24Parser.ObjectOrAttributePathContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitOidType([NotNull] Interlis24Parser.OidTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitParameterDef([NotNull] Interlis24Parser.ParameterDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitPathEl([NotNull] Interlis24Parser.PathElContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitPlausibilityConstraint([NotNull] Interlis24Parser.PlausibilityConstraintContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitProjection([NotNull] Interlis24Parser.ProjectionContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitProperties([NotNull] Interlis24Parser.PropertiesContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitProperty([NotNull] Interlis24Parser.PropertyContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitReferenceAttr([NotNull] Interlis24Parser.ReferenceAttrContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitRefSys([NotNull] Interlis24Parser.RefSysContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitRenamedViewableRef([NotNull] Interlis24Parser.RenamedViewableRefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitRoleDef([NotNull] Interlis24Parser.RoleDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitRotationDef([NotNull] Interlis24Parser.RotationDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitRunTimeParameterDef([NotNull] Interlis24Parser.RunTimeParameterDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitSelection([NotNull] Interlis24Parser.SelectionContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitSetConstraint([NotNull] Interlis24Parser.SetConstraintContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitSignParamAssignment([NotNull] Interlis24Parser.SignParamAssignmentContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitString([NotNull] Interlis24Parser.StringContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitTextType([NotNull] Interlis24Parser.TextTypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitTopicContents([NotNull] Interlis24Parser.TopicContentsContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitTopicDef([NotNull] Interlis24Parser.TopicDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitTopicRef([NotNull] Interlis24Parser.TopicRefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitType([NotNull] Interlis24Parser.TypeContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitUnion([NotNull] Interlis24Parser.UnionContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitUniqueEl([NotNull] Interlis24Parser.UniqueElContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitUniquenessConstraint([NotNull] Interlis24Parser.UniquenessConstraintContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitUnitDef([NotNull] Interlis24Parser.UnitDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitViewAttributes([NotNull] Interlis24Parser.ViewAttributesContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitViewDef([NotNull] Interlis24Parser.ViewDefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitRestrictedDefinitionRef([NotNull] Interlis24Parser.RestrictedDefinitionRefContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitExpNumber([NotNull] Interlis24Parser.ExpNumberContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitDecimalNumber([NotNull] Interlis24Parser.DecimalNumberContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitSignedNumber([NotNull] Interlis24Parser.SignedNumberContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitPosNumber([NotNull] Interlis24Parser.PosNumberContext context)
    {
        return ThrowNotImplementedException(context);
    }

    public virtual TResult VisitDomainTypeDef([NotNull] Interlis24Parser.DomainTypeDefContext context)
    {
        return ThrowNotImplementedException(context);
    }
}
