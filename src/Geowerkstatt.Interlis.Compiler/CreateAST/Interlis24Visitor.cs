using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools.AST.Expression;
using Geowerkstatt.Interlis.Tools.AST.Types;
using SharpCompress.Common;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Geowerkstatt.Interlis.Tools.CreateAST;

public sealed class Interlis24Visitor : ThrowingInterlis24ParserBaseVisitor<object>
{
    internal List<UnresolvedReference> ReferencesToResolve { get; } = new List<UnresolvedReference>();
    private Scope<IInterlisDefinitionContainer> CurrentScope = new Scope<IInterlisDefinitionContainer>();

    private IAntlrErrorListener<IToken> errorListener;

    internal Interlis24Visitor(IAntlrErrorListener<IToken> errorListener)
    {
        this.errorListener = errorListener;
    }

    private void ReportError(IToken offendingToken, string message)
    {
        errorListener.SyntaxError(null, null, offendingToken, offendingToken.Line, offendingToken.Column, message, null);
    }

    /// <summary>
    /// Add the <paramref name="referenceContext"/> to the references to resolve later.
    /// When the reference is resolved, the <paramref name="setSource"/> action is called with the result.
    /// </summary>
    private void DeferredReference(Interlis24Parser.DefinitionRefContext referenceContext, Action<IInterlisDefinition>? setSource)
    {
        if (referenceContext != null)
        {
            var reference = VisitDefinitionRef(referenceContext);
            reference.SetSource = setSource;
            reference.Source = CurrentScope.Value;
            ReferencesToResolve.Add(reference);
        }
    }

    /// <summary>
    /// Report an error if the given <paramref name="startName"/> and <paramref name="endName"/> do not match.
    /// </summary>
    private void CheckStartAndEndName(IToken offendingToken, string startName, string endName)
    {
        if (!string.Equals(startName, endName))
        {
            ReportError(offendingToken, $"Start name '{startName}' and end name '{endName}' do not match");
        }
    }

    /// <summary>
    /// Create a meta-attribute dictionary from <see cref="Interlis24Parser.MetaAttributeContext"/>s.
    /// </summary>
    private Dictionary<string, string> ProcessMetaAttributes(ParserRuleContext context, Interlis24Parser.MetaAttributesContext[] metaAttributeContexts)
    {
        var metaAttributeList = metaAttributeContexts
            .SelectMany(VisitMetaAttributes)
            .ToList();

        var duplicateMetaAttributes = metaAttributeList
            .GroupBy(m => m.Item1)
            .Where(g => g.Count() > 1)
            .Select(g => $"'{g.Key}'")
            .ToList();

        if (duplicateMetaAttributes.Count > 0)
        {
            ReportError(context.Start, $"{Interlis24Parser.ruleNames[context.RuleIndex]} has meta attributes with duplicate keys: {string.Join(", ", duplicateMetaAttributes)}");
        }

        return metaAttributeList.ToDictionary(m => m.Item1, m => m.Item2);
    }

    private void SetContentDictionary<T>(IContainer<IInterlisDefinition> container, IInterlisDefinitionContainer? parent, IToken token, IEnumerable<T> elements) where T : IInterlisDefinition
    {
        foreach (var element in elements)
        {
            element.Parent = parent;
            if (!container.Content.TryAdd(element.Name, element))
            {
                ReportError(token, $"An element with name {element.Name} already exists in the {(element.Parent == null ? "root scope" : "scope " + element.Parent.FullyQualifiedName)}");
            }
        }
    }

    public override InterlisFile VisitInterlis([NotNull] Interlis24Parser.InterlisContext context)
    {
        var interlisFile = new InterlisFile();
        SetContentDictionary(interlisFile, null, context.Start, context.modelDef().Select(VisitModelDef).Cast<IInterlisDefinition>());

        return interlisFile;
    }

    public override ModelDef VisitModelDef([NotNull] Interlis24Parser.ModelDefContext context)
    {
        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);

        var modelDef = new ModelDef
        {
            Name = context.name.Text,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
            Language = context.language?.Text,
            URI = VisitString(context.uri),
            Version = VisitString(context.modelVersion),
            Xmlns = context.xmlns == null ? null : VisitString(context.xmlns),
        };

        foreach (var import in context._imports)
        {
            if (!modelDef.Imports.TryAdd(import.name.Text, (import.UNQUALIFIED() != null, null)))
            {
                ReportError(import.name, $"Duplicate import {import.name.Text}");
            }
        }

        // Add default INTERLIS import
        modelDef.Imports.TryAdd("INTERLIS", (false, null));

        using var scopeFrame = CurrentScope.NewFrame(modelDef);
        var elements = context
            .modelContents()
            .SelectMany(c =>
            {
                var result = Visit(c);
                return result is IEnumerable collection ? collection.Cast<IInterlisDefinition>() : ([(IInterlisDefinition)result]);
            });

        SetContentDictionary(modelDef, modelDef, context.name, elements);
        return modelDef;
    }

    public override object VisitModelContents([NotNull] Interlis24Parser.ModelContentsContext context)
    {
        return VisitChildren(context);
    }

    public override TopicDef VisitTopicDef([NotNull] Interlis24Parser.TopicDefContext context)
    {
        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.FINAL]);

        var topicDef = new TopicDef
        {
            Name = context.name.Text,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
        };

        using var scopeFrame = CurrentScope.NewFrame(topicDef);

        DeferredReference(context.extends, e => topicDef.Extends = (TopicDef)e);
        DeferredReference(context.oid, e => topicDef.OidType = ((DomainDef)e).TypeDef);
        DeferredReference(context.basketOid, e => topicDef.BasketOidType = ((DomainDef)e).TypeDef);

        var elements = context
            .topicContents()
            .SelectMany(c =>
            {
                var result = Visit(c);
                return result is IEnumerable collection ? collection.Cast<IInterlisDefinition>() : ([(IInterlisDefinition)result]);
            });

        SetContentDictionary(topicDef, topicDef, context.name, elements);
        return topicDef;
    }

    public override object VisitTopicContents([NotNull] Interlis24Parser.TopicContentsContext context)
    {
        return VisitChildren(context);
    }

    public override ClassDef VisitClassDef([NotNull] Interlis24Parser.ClassDefContext context)
    {
        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL]);

        var classDef = new ClassDef
        {
            Name = context.name.Text,
            IsStructure = context.STRUCTURE() != null,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
        };

        DeferredReference(context.extends, e => classDef.Extends = (ClassDef)e);

        if (classDef.IsStructure && (context.oid != null || context.noOid != null))
        {
            ReportError(context.OID().Symbol, $"Structure '{classDef.Name}' cannot have an OID definition");
        }

        if (context.oid != null)
        {
            DeferredReference(context.oid, e => classDef.OidType = ((DomainDef)e).TypeDef);
        }
        else if (context.noOid != null)
        {
            classDef.OidType = new OidType { TypeDef = OidType.NoOid };
        }

        SetContentDictionary(classDef, classDef, context.name, VisitClassContent(context.classContent()));

        return classDef;
    }

    public override List<IInterlisDefinition> VisitClassContent([NotNull] Interlis24Parser.ClassContentContext context)
    {
        var constraints = context.constraintDef().Select(VisitConstraintDef).Cast<IInterlisDefinition>();
        var attributes = context.attributeDef().Select(VisitAttributeDef).Cast<IInterlisDefinition>();
        var parameters = context.parameterDef() == null ? null : VisitParameterDef(context.parameterDef());

        return attributes.Concat(constraints).ToList();
    }

    public override AssociationDef VisitAssociationDef([NotNull] Interlis24Parser.AssociationDefContext context)
    {
        CheckStartAndEndName(context.endName ?? context.Start, context.name?.Text ?? string.Empty, context.endName?.Text ?? string.Empty);
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.OID]);

        var roleDefs = context.roleDef().Select(VisitRoleDef).Cast<IInterlisDefinition>();
        var attributeDefs = context.attributeDef().Select(VisitAttributeDef).Cast<IInterlisDefinition>();
        var constraintDefs = context.constraintDef().Select(VisitConstraintDef).Cast<IInterlisDefinition>();

        var name = context.name?.Text ?? string.Concat(roleDefs.Select(r => r.Name));

        var associationDef = new AssociationDef
        {
            Name = name,
            Cardinality = context.cardinality() != null ? VisitCardinality(context.cardinality()) : new Cardinality { Min = 0, Max = Cardinality.Unbound },
        };

        SetContentDictionary(associationDef, associationDef, context.Start, roleDefs.Concat(attributeDefs).Concat(constraintDefs));

        return associationDef;
    }

    public override AttributeDef VisitRoleDef([NotNull] Interlis24Parser.RoleDefContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.HIDING, Interlis24Parser.ORDERED, Interlis24Parser.EXTERNAL]);

        // Read Cardinality
        Cardinality cardinality;
        var cardinalityContext = context.cardinality();
        var type = (Cardinality.RelationshipType)context.referenceType.Type;
        if (cardinalityContext == null)
        {
            cardinality = type switch
            {
                Cardinality.RelationshipType.Association => new Cardinality { Min = 0, Max = Cardinality.Unbound, Type = type },
                Cardinality.RelationshipType.Aggregation => new Cardinality { Min = 0, Max = Cardinality.Unbound, Type = type },
                Cardinality.RelationshipType.Composition => new Cardinality { Min = 0, Max = 1, Type = type },
                _ => throw new UnexpectedNodeException(context.referenceType)
            };
        }
        else
        {
            cardinality = VisitCardinality(context.cardinality()) with { Type = type };
        }

        if (type == Cardinality.RelationshipType.Composition && cardinality.Max > 1)
        {
            ReportError(context.referenceType, "Composition roles cannot have a maximum cardinality greater than 1");
        }

        // Read References
        var target = new RoleType { Cardinality = cardinality };
        foreach (var restrictedRef in context.restrictedDefinitionRef().Select(VisitRestrictedDefinitionRef))
        {
            var (referenceTarget, restrictions) = restrictedRef;
            var references = new RestrictedRef();

            referenceTarget.SetSource = e => references.Target = e;
            referenceTarget.Source = CurrentScope.Value;
            ReferencesToResolve.Add(referenceTarget);

            foreach (var restriction in restrictions)
            {
                restriction.SetSource = references.Restrictions.Add;
                restriction.Source = CurrentScope.Value;
                ReferencesToResolve.Add(restriction);
            }

            target.Targets.Add(references);
        }

        return new AttributeDef
        {
            Name = context.name.Text,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
            TypeDef = target,
        };
    }

    public override Tuple<UnresolvedReference, List<UnresolvedReference>> VisitRestrictedDefinitionRef([NotNull] Interlis24Parser.RestrictedDefinitionRefContext context)
    {
        var target = VisitDefinitionRef(context.@ref);
        var restrictions = context._restrictions.Select(VisitDefinitionRef).ToList();

        return Tuple.Create(target, restrictions);
    }

    public override UnresolvedReference VisitDefinitionRef([NotNull] Interlis24Parser.DefinitionRefContext context)
    {
        return new UnresolvedReference
        {
            Target = { new[] { context.model?.Text, context.topic?.Text, context.name.Text }.WhereNotNull() },
        };
    }

    public override AttributeDef VisitAttributeDef([NotNull] Interlis24Parser.AttributeDefContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.TRANSIENT]);

        return new AttributeDef
        {
            Name = context.name.Text,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
            TypeDef = VisitAttrTypeDef(context.attrTypeDef()),
        };
    }

    public override TypeDef VisitAttrTypeDef([NotNull] Interlis24Parser.AttrTypeDefContext context)
    {
        Cardinality cardinality;
        if (context.MANDATORY() != null)
        {
            cardinality = new Cardinality { Min = 1, Max = 1 };
        }
        else
        {
            if (context.OF() == null)
            {
                cardinality = new Cardinality { Min = 0, Max = 1 };
            }
            else
            {
                var isOrdered = context.LIST != null;
                var cardinalityContext = context.cardinality();
                if (cardinalityContext == null)
                {
                    cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound, Ordered = isOrdered };
                }
                else
                {
                    cardinality = VisitCardinality(cardinalityContext) with { Ordered = isOrdered };
                }
            }
        }

        var type = VisitAttrType(context.attrType());
        type.Cardinality = cardinality;

        return type;
    }

    public override TypeDef VisitAttrType([NotNull] Interlis24Parser.AttrTypeContext context)
    {
        var result = VisitChildren(context);
        if (result is Tuple<UnresolvedReference, List<UnresolvedReference>> reference)
        {
            var (target, restrictions) = reference;
            var restrictedRef = new RestrictedRef();
            target.SetSource = e => restrictedRef.Target = e;
            target.Source = CurrentScope.Value;
            ReferencesToResolve.Add(target);

            foreach (var restriction in restrictions)
            {
                restriction.SetSource = restrictedRef.Restrictions.Add;
                restriction.Source = CurrentScope.Value;
                ReferencesToResolve.Add(restriction);
            }

            return new ReferenceType
            {
                Target = restrictedRef,
            };
        }
        else
        {
            return (TypeDef)result;
        }
    }

    public override object VisitType([NotNull] Interlis24Parser.TypeContext context)
    {
        return VisitChildren(context);
    }

    public override object VisitBaseType([NotNull] Interlis24Parser.BaseTypeContext context)
    {
        return VisitChildren(context);
    }

    public override TextType VisitTextType([NotNull] Interlis24Parser.TextTypeContext context)
    {
        return new TextType
        {
            Length = int.Parse(context.maxLength.Text),
        };
    }

    public override NumericType VisitNumericType([NotNull] Interlis24Parser.NumericTypeContext context)
    {
        var numericTypeDef = new NumericType()
        {
            Circular = context.CIRCULAR() != null,
        };

        if (context.min != null)
        {
            var (minValue, minPrecision) = (Tuple<double, int>)Visit(context.min);
            var (maxValue, maxPrecision) = (Tuple<double, int>)Visit(context.max);

            if (minPrecision != maxPrecision)
            {
                ReportError(context.Start, $"Number minimum and maximum must have the same precision but minimum has precision <{Math.Pow(10, minPrecision)}> and maximum has precision <{Math.Pow(10, maxPrecision)}>");
            }

            if (minValue > maxValue)
            {
                ReportError(context.Start, $"Number minimum <{minValue}> must be smaller than maximum <{maxValue}>");
                (minValue, maxValue) = (maxValue, minValue);
            }

            // Check if it might be ok to represent the values as a double
            var delta = Math.Pow(10, minPrecision);
            if ((maxValue - Math.BitDecrement(maxValue)) >= delta || (Math.BitIncrement(minValue) - minValue) >= delta)
            {
                ReportError(context.Start, $"The given range <{minValue} .. {maxValue}> with a precision of <{delta}> cannot be represented by a double precision floating point number");
            }

            numericTypeDef.Min = minValue;
            numericTypeDef.Max = maxValue;
            numericTypeDef.Precision = minPrecision;
        }

        DeferredReference(context.unit, e => numericTypeDef.Unit = (UnitDef)e);

        return numericTypeDef;
    }

    public override Tuple<double, int> VisitExpNumber([NotNull] Interlis24Parser.ExpNumberContext context)
    {
        var number = context.EXP_NUMBER().Symbol.Text;
        var decPointIndex = number.IndexOf('.');
        var expIndex = number.IndexOfAny(['e', 'E']);

        var decimalPrecision = expIndex - decPointIndex - 1;
        var exp = int.Parse(number.Substring(expIndex + 1));

        var value = double.Parse(number);
        var precision = exp - decimalPrecision;

        return Tuple.Create(value, precision);
    }

    public override Tuple<double, int> VisitDecimalNumber([NotNull] Interlis24Parser.DecimalNumberContext context)
    {
        var number = context.DECIMAL_NUMBER().Symbol.Text;
        var decPointIndex = number.IndexOf('.');

        var value = double.Parse(number);
        var precision = decPointIndex - number.Length + 1;

        return Tuple.Create(value, precision);
    }

    public override Tuple<double, int> VisitSignedNumber([NotNull] Interlis24Parser.SignedNumberContext context)
    {
        return Tuple.Create(double.Parse(context.SIGNED_NUMBER().Symbol.Text), 0);
    }

    public override Tuple<double, int> VisitPosNumber([NotNull] Interlis24Parser.PosNumberContext context)
    {
        return Tuple.Create(double.Parse(context.POS_NUMBER().Symbol.Text), 0);
    }

    public override BooleanType VisitBooleanType([NotNull] Interlis24Parser.BooleanTypeContext context)
    {
        return new BooleanType();
    }

    public override CoordType VisitCoordinateType([NotNull] Interlis24Parser.CoordinateTypeContext context)
    {
        if (context.rotationDef() != null)
        {
            Visit(context.rotationDef());
        }

        if (context.refsys != null)
        {
            throw new NotImplementedException();
        }

        return new CoordType
        {
            IsMultiGeometry = context.MULTICOORD() != null,
            Axis = { context._axis.Select(VisitNumericType) },
        };
    }

    public override List<UnitDef> VisitUnitDef([NotNull] Interlis24Parser.UnitDefContext context)
    {
        return context.unitTypeDef().Select(VisitUnitTypeDef).ToList();
    }

    public override UnitDef VisitUnitTypeDef([NotNull] Interlis24Parser.UnitTypeDefContext context)
    {
        var term = context.unitTerm.Text;
        var shortName = context.unitShortName?.Text;

        var unit = new UnitDef
        {
            Name = shortName ?? term,
            Term = term,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
        };

        DeferredReference(context.extends, e => unit.Extends = (UnitDef)e);

        return unit;
    }

    public override List<DomainDef> VisitDomainDef([NotNull] Interlis24Parser.DomainDefContext context)
    {
        return context.domainTypeDef().Select(VisitDomainTypeDef).ToList();
    }

    public override DomainDef VisitDomainTypeDef([NotNull] Interlis24Parser.DomainTypeDefContext context)
    {
        var typeContext = context.type();
        var type = typeContext != null ? (TypeDef)VisitType(typeContext) : new TypeRef();
        type.Cardinality = new Cardinality { Min = context.MANDATORY() == null ? 0 : 1, Max = Cardinality.Unbound };
        type.Constraints.Add(context.domainConstraint().Select(VisitDomainConstraint));

        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.GENERIC, Interlis24Parser.FINAL]);

        DeferredReference(context.extends, e => type.Extends = ((DomainDef)e).TypeDef);

        return new DomainDef
        {
            Name = context.name.Text,
            TypeDef = type,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
        };
    }

    public override DomainConstraint VisitDomainConstraint([NotNull] Interlis24Parser.DomainConstraintContext context)
    {
        return new DomainConstraint
        {
            Name = context.IDENTIFIER().GetText(),
            Condition = (IExpression)Visit(context.expression()),
        };
    }

    public override TypeDef VisitOidType([NotNull] Interlis24Parser.OidTypeContext context)
    {
        return new OidType
        {
            TypeDef = context.ANY() != null ? new OidAnyType() : (TypeDef)VisitChildren(context),
        };
    }

    public override TypeDef VisitBlackboxType([NotNull] Interlis24Parser.BlackboxTypeContext context)
    {
        return new BlackboxType
        {
            Kind = context.XML() != null ? BlackboxType.BlackboxTypeKind.Xml : BlackboxType.BlackboxTypeKind.Binary,
        };
    }

    public override TypeDef VisitLineType([NotNull] Interlis24Parser.LineTypeContext context)
    {
        var lineForm = context.lineForm() != null ? VisitLineForm(context.lineForm()) : Enumerable.Empty<string>();
        var overlap = context.numeric() != null ? ((Tuple<double, int>)Visit(context.numeric())).Item1 : 0.0;

        if (context.POLYLINE() != null || context.MULTIPOLYLINE() != null)
        {
            var line = new PolyLineType
            {
                IsMultiGeometry = context.MULTIPOLYLINE() != null,
                IsDirected = context.DIRECTED() != null,
                OverlapTolerance = overlap,
                LineForm = { lineForm },
            };

            DeferredReference(context.vertexType, e => line.VertexType = ((DomainDef)e).TypeDef);
            return line;
        }
        else
        {
            var surface = new SurfaceType
            {
                IsMultiGeometry = context.MULTIAREA() != null || context.MULTISURFACE() != null,
                IsCoverage = context.AREA() != null || context.MULTIAREA() != null,
                OverlapTolerance = overlap,
                LineForm = { lineForm },
            };

            DeferredReference(context.vertexType, e => surface.VertexType = ((DomainDef)e).TypeDef);
            return surface;
        }
    }

    public override HashSet<string> VisitLineForm([NotNull] Interlis24Parser.LineFormContext context)
    {
        // Duplicates are silently ignored!
        return context.lineFormType().Select(VisitLineFormType).ToHashSet();
    }

    public override string VisitLineFormType([NotNull] Interlis24Parser.LineFormTypeContext context)
    {
        if (context.definitionRef() != null) throw new NotImplementedException("Custom LineFormType not implemented");
        return context.Start.Text;
    }

    public override EnumerationAllOfType VisitEnumTreeValueType([NotNull] Interlis24Parser.EnumTreeValueTypeContext context)
    {
        var enumerationAllOfType = new EnumerationAllOfType();
        DeferredReference(context.definitionRef(), e => enumerationAllOfType.TargetEnumeration = (EnumerationType)((DomainDef)e).TypeDef);
        return enumerationAllOfType;
    }

    public override EnumerationType VisitEnumerationType([NotNull] Interlis24Parser.EnumerationTypeContext context)
    {
        return new EnumerationType
        {
            Sequencing = (EnumerationType.Sequencings)(context.sequencing?.Type ?? 0),
            Values = { VisitEnumeration(context.enumeration()) },
        };
    }

    public override EnumerationValuesList VisitEnumeration([NotNull] Interlis24Parser.EnumerationContext context)
    {
        var values = new EnumerationValuesList { context.enumElement().Select(VisitEnumElement) };
        values.IsFinal = context.FINAL() != null;
        return values;
    }

    public override EnumerationTreeNode VisitEnumElement([NotNull] Interlis24Parser.EnumElementContext context)
    {
        var identifiers = context.IDENTIFIER();
        if (identifiers.Length == 0)
        {
            throw new UnexpectedNodeException(context, $"{nameof(context.IDENTIFIER)} missing");
        }

        EnumerationTreeNode root, leaf = root = new EnumerationTreeNode { Name = identifiers.First().GetText() };
        foreach (var value in identifiers.Skip(1))
        {
            var node = new EnumerationTreeNode { Name = value.GetText() };
            leaf.SubValues.Add(node);
            leaf = node;
        }

        leaf.DocComments.Add(context.DOC_COMMENT().Select(d => d.GetText()));
        leaf.MetaAttributes.Add(ProcessMetaAttributes(context, context.metaAttributes()));
        if (context.enumeration() != null)
        {
            leaf.SubValues.Add(VisitEnumeration(context.enumeration()));
        }

        return root;
    }

    public override List<Tuple<string, string>> VisitMetaAttributes([NotNull] Interlis24Parser.MetaAttributesContext context)
    {
        return context.metaAttribute().Select(VisitMetaAttribute).ToList();
    }

    public override Tuple<string, string> VisitMetaAttribute([NotNull] Interlis24Parser.MetaAttributeContext context)
    {
        var key = context.GetChild(0).GetText();
        var valueContext = context.GetChild(2);
        var value = (string)Visit(valueContext) ?? valueContext.GetText();

        return Tuple.Create(key, value);
    }

    public override string VisitString([NotNull] Interlis24Parser.StringContext context)
    {
        var sb = new StringBuilder();
        // Skip first and last child as those are the string quotes
        for (int i = 1; i < context.ChildCount - 1; i++)
        {
            var child = context.children[i];
            if (child is TerminalNodeImpl terminal)
            {
                // Terminal symbols don't have individual visit methods.
                switch (terminal.Symbol.Type)
                {
                    case Interlis24Parser.LITERAL_TEXT:
                        sb.Append(terminal.GetText());
                        break;

                    case Interlis24Parser.BACKSLASH:
                        sb.Append('\\');
                        break;

                    case Interlis24Parser.DOUBLE_QUOTE:
                        sb.Append('"');
                        break;

                    case Interlis24Parser.UNICODE:
                        // Remove the '\u' prefix
                        var hexString = terminal.GetText().Substring(2);
                        var utf16char = (char)int.Parse(hexString, NumberStyles.HexNumber);
                        sb.Append(utf16char);
                        break;

                    case Interlis24Parser.UNKNOWN_ESCAPE:
                        ReportError(terminal.Symbol, $"Invalid escape sequence inside String: '{terminal.GetText()}'");
                        sb.Append(terminal.GetText());
                        break;

                    case Interlis24Parser.INVALID_UNICODE:
                        ReportError(terminal.Symbol, $"Unicode escape sequence with invalid characters: '{terminal.GetText()}'");
                        sb.Append(terminal.GetText());
                        break;

                    default:
                        throw new UnexpectedNodeException(child);
                }
            }
            else
            {
                throw new UnexpectedNodeException(child, "Expected only terminals inside string literal.");
            }
        }

        return sb.ToString();
    }

    public override Cardinality VisitCardinality([NotNull] Interlis24Parser.CardinalityContext context)
    {
        long? parseCardinalityValue(IToken token)
        {
            if (token.Type != Interlis24Parser.ASTERISK)
            {
                if (long.TryParse(token.Text, out long value))
                {
                    return value;
                }
                else
                {
                    ReportError(token, $"Could not parse value {token.Text}");
                }
            }
            return Cardinality.Unbound;
        }

        var fromValue = parseCardinalityValue(context.from);
        long? toValue;
        if (context.to == null)
        {
            toValue = fromValue;
            if (fromValue == Cardinality.Unbound)
            {
                fromValue = 0;
            }
        }
        else
        {
            toValue = parseCardinalityValue(context.to);
            if (fromValue == Cardinality.Unbound)
            {
                ReportError(context.from, $"Invalid cardinality '{context.GetText()}', did you mean '{{0..{context.to.Text}}}'");
                fromValue = 0;
            }

            if (toValue != Cardinality.Unbound && fromValue > toValue)
            {
                ReportError(context.Start, $"Invalid cardinality minimal value '{fromValue}' is larger than maximal value '{toValue}'");
                (fromValue, toValue) = (toValue, fromValue);
            }
        }

        return new Cardinality
        {
            Min = fromValue,
            Max = toValue,
        };
    }

    /// <summary>
    /// Get a collection of <see cref="IToken.Type"/> from the <see cref="Interlis24Parser.PropertiesContext"/>.
    /// </summary>
    private HashSet<int> VisitProperties(Interlis24Parser.PropertiesContext context, int[] allowedProperties)
    {
        var result = new HashSet<int>();
        if (context == null) return result;

        var properties = VisitProperties(context);
        foreach (var property in properties)
        {
            if (!result.Add(property.Type))
            {
                ReportError(property, $"Duplicate property {property.Text}");
            }

            if (!allowedProperties.Contains(property.Type))
            {
                var allowedPropString = string.Join(", ", allowedProperties.Select(p => Interlis24Parser.DefaultVocabulary.GetDisplayName(p)));
                ReportError(property, $"Property '{property.Text}' is not one of the allowed properties ({allowedPropString})");
            }
        }

        return result;
    }

    public override List<IToken> VisitProperties([NotNull] Interlis24Parser.PropertiesContext context)
    {
        return context.property().Select(VisitProperty).ToList();
    }

    public override IToken VisitProperty([NotNull] Interlis24Parser.PropertyContext context)
    {
        return context.Start;
    }

    public override IExpression VisitBinaryExpression([NotNull] Interlis24Parser.BinaryExpressionContext context)
    {
        var first = (IExpression)Visit(context.expression(0));
        var second = (IExpression)Visit(context.expression(1));

        return (context.binOp.Type) switch
        {
            Interlis24Parser.DOUBLE_EQUAL => new EqualExpression { FirstOperand = first, SecondOperand = second },
            Interlis24Parser.NOT_EQUAL => new NotExpression { Operand = new EqualExpression { FirstOperand = first, SecondOperand = second } },
            Interlis24Parser.GREATER => new GreaterThanExpression { FirstOperand = first, SecondOperand = second },
            Interlis24Parser.LESSER => new GreaterThanExpression { FirstOperand = second, SecondOperand = first },
            Interlis24Parser.GREATER_EQUAL => new NotExpression { Operand = new GreaterThanExpression { FirstOperand = second, SecondOperand = first } },
            Interlis24Parser.LESS_EQUAL => new NotExpression { Operand = new GreaterThanExpression { FirstOperand = first, SecondOperand = second } },

            Interlis24Parser.AND => new AndExpression { FirstOperand = first, SecondOperand = second },
            Interlis24Parser.OR => new OrExpression { FirstOperand = first, SecondOperand = second },
            Interlis24Parser.FAT_ARROW => new OrExpression { FirstOperand = new NotExpression { Operand = first }, SecondOperand = second },

            Interlis24Parser.PLUS => new Addition { FirstOperand = first, SecondOperand = second },
            Interlis24Parser.HYPHEN => new Subtraction { FirstOperand = first, SecondOperand = second },
            Interlis24Parser.ASTERISK => new Multiplication { FirstOperand = first, SecondOperand = second },
            Interlis24Parser.SLASH => new Division { FirstOperand = first, SecondOperand = second },

            _ => throw new NotImplementedException($"Operator {Interlis24Parser.DefaultVocabulary.GetDisplayName(context.binOp.Type)} not implemented"),
        };
    }

    public override IExpression VisitFactorExpression([NotNull] Interlis24Parser.FactorExpressionContext context)
    {
        return VisitFactor(context.factor());
    }

    public override IExpression VisitNotExpression([NotNull] Interlis24Parser.NotExpressionContext context)
    {
        var expression = (IExpression)Visit(context.expression());
        return context.NOT() == null ? expression : new NotExpression { Operand = expression };
    }

    public override IExpression VisitDefinedExpression([NotNull] Interlis24Parser.DefinedExpressionContext context)
    {
        return new DefinedExpression { Operand = VisitFactor(context.factor()) };
    }

    public override IExpression VisitFactor([NotNull] Interlis24Parser.FactorContext context)
    {
        return (IExpression)VisitChildren(context);
    }

    public override ConstantExpression VisitConstant([NotNull] Interlis24Parser.ConstantContext context)
    {
        if (context.@string() != null)
        {
            var value = VisitString(context.@string());
            return new TextConstant { Value = value };
        }
        else if (context.UNDEFINED() != null)
        {
            return new UndefinedConstant();
        }
        else
        {
            return (ConstantExpression)VisitChildren(context);
        }
    }

    public override NumericConstant VisitNumericConst([NotNull] Interlis24Parser.NumericConstContext context)
    {
        if (context.definitionRef() != null) throw new NotImplementedException("Unit not supported");

        var (value, precision) = VisitDecConst(context.decConst());
        return new NumericConstant { Value = value };
    }

    public override EnumerationConstant VisitEnumerationConst([NotNull] Interlis24Parser.EnumerationConstContext context)
    {
        var path = context.IDENTIFIER().Select(e => e.GetText()).ToList();
        if (context.OTHERS() != null)
        {
            path.Add(EnumerationConstant.Others);
        }

        return new EnumerationConstant { Path = { path } };
    }

    public override Tuple<double, int> VisitDecConst([NotNull] Interlis24Parser.DecConstContext context)
    {
        if (context.PI() != null) return Tuple.Create(Math.PI, -16);
        if (context.LNBASE() != null) return Tuple.Create(Math.E, -16);

        return (Tuple<double, int>)Visit(context.numeric());
    }
}
