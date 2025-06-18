using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Visitor that creates an Abstract-Syntax-Tree (AST) from the output of ANTLR.
/// </summary>
/// <param name="loggerFactory">The Factory to create logger instances.</param>
/// <param name="tokenStream">The <see cref="CommonTokenStream"/> to access hidden tokens.</param>
public sealed class Interlis24Visitor(ILoggerFactory loggerFactory, CommonTokenStream tokenStream) : LoggingInterlis24ParserBaseVisitor<object>(loggerFactory)
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Interlis24Visitor>();

    private Scope<IInterlisDefinitionContainer> CurrentScope = new Scope<IInterlisDefinitionContainer>();

    /// <summary>
    /// Report an error at the position of the <paramref name="offendingToken"/>.
    /// </summary>
    /// <param name="offendingToken">The <see cref="IToken"/> that caused the error.</param>
    /// <param name="message">The error message.</param>
    private void ReportError(IToken offendingToken, string message)
    {
        logger.LogError("Compile error at line {Line}:{CharPosition} {Message}.", offendingToken.Line, offendingToken.Column, message);
    }

    /// <summary>
    /// Create a new <see cref="Reference{T}"/> from the given <paramref name="referenceContext"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(referenceContext))]
    private Reference<T>? CreateReference<T>(Interlis24Parser.DefinitionRefContext? referenceContext, Func<IInterlisDefinition, T?>? mapTarget = null) where T : class, IInterlisDefinition
    {
        return referenceContext == null ? null : CreateReference<T>(VisitDefinitionRef(referenceContext), GetRange(referenceContext), mapTarget);
    }

    /// <summary>
    /// Create a new <see cref="Reference{T}"/> with the given <paramref name="path"/>.
    /// </summary>
    private Reference<T> CreateReference<T>(IEnumerable<string> path, RangePosition? location = null, Func<IInterlisDefinition, T?>? mapTarget = null) where T : class, IInterlisDefinition
    {
        var reference = new Reference<T>
        {
            Path = { path },
            Source = CurrentScope.Value,
            MapTarget = mapTarget ?? (element => element as T),
            ReferenceLocation = location,
        };

        CurrentScope.Value?.ContainerReferences.Add(reference);

        return reference;
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
    /// Create a <see cref="RangePosition"/> from the given <paramref name="token"/>.
    /// </summary>
    /// <remarks>Only works correctly if the <paramref name="token"/> does not span multiple lines.</remarks>
    private RangePosition GetRange(IToken token)
    {
        return new RangePosition
        {
            Start = new Position { Line = token.Line - 1, Character = token.Column },
            End = new Position { Line = token.Line - 1, Character = token.Column + token.Text.Length },
        };
    }

    /// <summary>
    /// Create a <see cref="RangePosition"/> from the given <paramref name="context"/>.
    /// </summary>
    /// <remarks>Only works correctly if the last <paramref name="token"/> does not span multiple lines.</remarks>
    private RangePosition GetRange(ParserRuleContext context)
    {
        return new RangePosition
        {
            Start = new Position { Line = context.Start.Line - 1, Character = context.Start.Column },
            End = new Position { Line = context.Stop.Line - 1, Character = context.Stop.Column + context.Stop.Text.Length },
        };
    }

    /// <summary>
    /// Create a meta-attribute dictionary from the meta comments preceding the specified <paramref name="context"/>.
    /// </summary>
    public Dictionary<string, string> ProcessMetaAttributes(ParserRuleContext context)
    {
        var metaCommentTokens = (tokenStream.GetHiddenTokensToLeft(context.Start.TokenIndex, Interlis24Lexer.META_COMMENT) ?? Enumerable.Empty<IToken>()).ToList();
        if (metaCommentTokens.Any())
        {
            var interlisParser = new Interlis24Parser(new CommonTokenStream(new ListTokenSource(metaCommentTokens), Interlis24Lexer.META_COMMENT));
            interlisParser.RemoveErrorListeners();
            interlisParser.AddErrorListener(new ILoggerParserErrorListener(loggerFactory));

            var metaAttributeList = VisitMetaComments(interlisParser.metaComments());

            var duplicateMetaAttributes = metaAttributeList
                .GroupBy(m => m.Item1)
                .Where(g => g.Count() > 1)
                .Select(g => $"'{g.Key}'")
                .ToList();

            if (duplicateMetaAttributes.Count == 0)
            {
                return metaAttributeList.ToDictionary(m => m.Item1, m => m.Item2);
            }
            else
            {
                ReportError(context.Start, $"{Interlis24Parser.ruleNames[context.RuleIndex]} has meta attributes with duplicate keys: {string.Join(", ", duplicateMetaAttributes)}");
            }
        }

        return new Dictionary<string, string>();
    }

    /// <summary>
    /// Get the documentation comments preceding the given <paramref name="context"/>.
    /// </summary>
    private IList<string> GetDocComments(ParserRuleContext context)
    {
        return (tokenStream.GetHiddenTokensToLeft(context.Start.TokenIndex) ?? Enumerable.Empty<IToken>())
            .Where(t => t.Type == Interlis24Lexer.DOC_COMMENT)
            .Select(t => t.Text)
            .ToList();
    }

    private void SetContentDictionary<T>(IContainer<T> container, IInterlisDefinitionContainer? parent, IToken token, IEnumerable<T> elements) where T : class, IInterlisDefinition
    {
        foreach (var element in elements.WhereNotNull())
        {
            element.Parent = parent;
            if (!container.Content.TryAdd(element.Name, element))
            {
                ReportError(token, $"An element with name {element.Name} already exists in the {(element.Parent == null ? "root scope" : "scope " + element.Parent.FullyQualifiedName)}");
            }
        }
    }

    public override InterlisEnvironment VisitInterlis([NotNull] Interlis24Parser.InterlisContext context)
    {
        double? version = context.numeric() == null ? null : ((Tuple<double, int>)Visit(context.numeric())).Item1;

        var interlisFile = new InterlisEnvironment
        {
            Version = version,
            Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } },
        };

        if (version != 2.4)
        {
            logger.LogWarning("Unsupported INTERLIS version {Version}. Only version 2.4 is supported.", version);
        }

        SetContentDictionary(interlisFile, null, context.Start, context.modelDef().Select(VisitModelDef));
        return interlisFile;
    }

    public override ModelDef VisitModelDef([NotNull] Interlis24Parser.ModelDefContext context)
    {
        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);

        var modelDef = new ModelDef
        {
            Name = context.name.Text,
            NameLocations = { GetRange(context.name), GetRange(context.endName) },
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Language = context.language?.Text,
            URI = VisitString(context.uri),
            Version = VisitString(context.modelVersion),
            Xmlns = context.xmlns == null ? null : VisitString(context.xmlns),
        };

        using var scopeFrame = CurrentScope.NewFrame(modelDef);
        var importedModels = new HashSet<string>();
        foreach (var import in context._imports)
        {
            var importModelName = import.name.Text;
            if (!modelDef.Imports.TryAdd(importModelName, (import.UNQUALIFIED() != null, CreateReference<ModelDef>([importModelName], GetRange(import.name)))))
            {
                ReportError(import.name, $"Duplicate import {importModelName}");
            }
        }

        // Add default INTERLIS import
        modelDef.Imports.TryAdd("INTERLIS", (false, CreateReference<ModelDef>(["INTERLIS"])));

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
            NameLocations = { GetRange(context.name), GetRange(context.endName) },
            Extends = CreateReference<TopicDef>(context.extends),
            OidType = CreateReference<DomainDef>(context.oid),
            BasketOidType = CreateReference<DomainDef>(context.basketOid),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { properties },
        };

        using var scopeFrame = CurrentScope.NewFrame(topicDef);

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
            NameLocations = { GetRange(context.name), GetRange(context.endName) },
            IsStructure = context.STRUCTURE() != null,
            Extends = CreateReference<ClassDef>(context.extends),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { properties },
        };

        if (classDef.IsStructure && (context.oid != null || context.noOid != null))
        {
            ReportError(context.OID().Symbol, $"Structure '{classDef.Name}' cannot have an OID definition");
        }

        if (context.oid != null)
        {
            classDef.OidType = CreateReference<DomainDef>(context.oid);
        }
        else if (context.noOid != null)
        {
            classDef.OidType = CreateReference<DomainDef>(["INTERLIS", "NOOID"]);
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
            NameLocations = { new [] { context.name, context.endName }.WhereNotNull().Select(GetRange) },
            Extends = CreateReference<AssociationDef>(context.extends),
            Cardinality = context.cardinality() != null ? VisitCardinality(context.cardinality()) : new Cardinality { Min = 0, Max = Cardinality.Unbound },
            Properties = { properties },
        };

        if (context.oid != null)
        {
            associationDef.OidType = CreateReference<DomainDef>(context.oid);
        }
        else if (context.noOid != null)
        {
            associationDef.OidType = CreateReference<DomainDef>(["INTERLIS", "NOOID"]);
        }

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
            target.Targets.Add(restrictedRef);
        }

        return new AttributeDef
        {
            Name = context.name.Text,
            NameLocations = { GetRange(context.name) },
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            TypeDef = target,
            Properties = { properties },
        };
    }

    public override object VisitReferenceAttr([NotNull] Interlis24Parser.ReferenceAttrContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.EXTERNAL]);

        return new ReferenceType
        {
            Target = VisitRestrictedDefinitionRef(context.restrictedDefinitionRef()),
            Properties = { properties },
            SourceRange = GetRange(context),
        };
    }

    public override RestrictedRef VisitRestrictedDefinitionRef([NotNull] Interlis24Parser.RestrictedDefinitionRefContext context)
    {
        // RestrictedDefinitionRef only accepts InterlisDefinitions of certain types
        Func<IInterlisDefinition, IInterlisDefinition?> acceptTypes = interlisDef => interlisDef switch
        {
            ClassDef c => c,
            AssociationDef a => a,
            DomainDef d => d, // Domains cannot be restricted, but are accepted because of an ambiguity in 'attrType' that can only be resolved when the target type of the reference is known.
            _ => null,
        };

        return new RestrictedRef
        {
            Value = CreateReference(context.@ref, acceptTypes),
            Restrictions = { context._restrictions.Select(r => CreateReference(r, acceptTypes)).WhereNotNull() },
        };
    }

    public override IEnumerable<string> VisitDefinitionRef([NotNull] Interlis24Parser.DefinitionRefContext context)
    {
        return new[] { context.model?.Text, context.topic?.Text, context.name.Text }.WhereNotNull();
    }

    public override AttributeDef VisitAttributeDef([NotNull] Interlis24Parser.AttributeDefContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.TRANSIENT]);

        return new AttributeDef
        {
            Name = context.name.Text,
            NameLocations = { GetRange(context.name) },
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            TypeDef = VisitAttrTypeDef(context.attrTypeDef()),
            Properties = { properties },
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
        var restrictedDefinitonRef = context.restrictedDefinitionRef();
        if (restrictedDefinitonRef != null)
        {
            return new ReferenceType
            {
                Target = VisitRestrictedDefinitionRef(restrictedDefinitonRef),
                SourceRange = GetRange(context),
            };
        }
        else
        {
            return (TypeDef)VisitChildren(context);
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

    public override TypeDef VisitTextType([NotNull] Interlis24Parser.TextTypeContext context)
    {
        if (context.TEXT() != null || context.MTEXT() != null)
        {
            return new TextType
            {
                Length = context.maxLength != null ? int.Parse(context.maxLength.Text) : null,
                IsMText = context.MTEXT() != null,
                SourceRange = GetRange(context),
            };
        }
        else
        {
            return new TypeRef { Extends = CreateReference<DomainDef>(["INTERLIS", context.GetText()]), SourceRange = GetRange(context), };
        }
    }

    public override NumericType VisitNumericType([NotNull] Interlis24Parser.NumericTypeContext context)
    {
        var numericTypeDef = new NumericType()
        {
            Circular = context.CIRCULAR() != null,
            Unit = CreateReference<UnitDef>(context.unit),
            SourceRange = GetRange(context),
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

        return numericTypeDef;
    }

    public override FormattedType VisitFormattedType([NotNull] Interlis24Parser.FormattedTypeContext context)
    {
        return new FormattedType
        {
            Min = context.min == null ? null : VisitString(context.min),
            Max = context.max == null ? null : VisitString(context.max),
            BasedOn = CreateReference<ClassDef>(context.basedOn),
            FormatBaseType = CreateReference<DomainDef>(context.domainRef),
            SourceRange = GetRange(context),
        };
    }

    public override TypeDef VisitDateTimeType([NotNull] Interlis24Parser.DateTimeTypeContext context)
    {
        var domainName = context.kind.Type switch
        {
            Interlis24Parser.DATE => "XMLDate",
            Interlis24Parser.TIMEOFDAY => "XMLTime",
            Interlis24Parser.DATETIME => "XMLDateTime",
            _ => throw new UnexpectedNodeException(context.kind),
        };

        return new TypeRef { Extends = CreateReference<DomainDef>(["INTERLIS", domainName]) };
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
        return new BooleanType
        {
            SourceRange = GetRange(context),
        };
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
            SourceRange = GetRange(context),
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
            NameLocations = { GetRange(context.unitShortName == null ? context.unitTerm : context.unitShortName) },
            Extends = CreateReference<UnitDef>(context.extends),
            Term = term,
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { context.ABSTRACT() != null ? [Property.Abstract] : [] },
            Expression = context switch
            {
                var ctx when ctx.derivedUnit() is { } d => VisitDerivedUnit(d),
                var ctx when ctx.composedUnit() is { } c => VisitComposedUnit(c),
                _ => null
            },
        };

        return unit;
    }

    public override IExpression VisitDerivedUnit([NotNull] Interlis24Parser.DerivedUnitContext context)
    {
        var derivedFrom = new PathExpression { Path = { new ReferencePathElement { Value = CreateReference<IInterlisDefinition>(context.definitionRef()) } } };

        if (context.FUNCTION() != null)
        {
            return null!;
        }

        var constants = context.decConst().Select(n => { var (value, precision) = VisitDecConst(n); return value; }).ToArray();
        if (constants.Length == 0)
        {
            return derivedFrom;
        }
        else
        {
            var constant = constants[0];
            for (var i = 1; i < constants.Length; i++)
            {
                constant = context._op[i - 1].Type switch
                {
                    Interlis24Parser.ASTERISK => constant * constants[i],
                    Interlis24Parser.SLASH => constant / constants[i],
                    _ => throw new UnexpectedNodeException(context._op[i - 1]),
                };
            }

            return new Multiplication { FirstOperand = new NumericConstant { Value = constant }, SecondOperand = derivedFrom };
        }
    }

    public override IExpression VisitComposedUnit([NotNull] Interlis24Parser.ComposedUnitContext context)
    {
        var composedFrom = context.definitionRef().Select(d => new PathExpression { Path = { new ReferencePathElement { Value = CreateReference<IInterlisDefinition>(d) } } }).ToArray();
        IExpression result = composedFrom[0];
        for (var i = 1; i < composedFrom.Length; i++)
        {
            result = context._op[i - 1].Type switch
            {
                Interlis24Parser.ASTERISK => new Multiplication { FirstOperand = result, SecondOperand = composedFrom[i] },
                Interlis24Parser.SLASH => new Division { FirstOperand = result, SecondOperand = composedFrom[i] },
                _ => throw new UnexpectedNodeException(context._op[i - 1]),
            };
        }

        return result;
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

        type.Extends = CreateReference<DomainDef>(context.extends);

        return new DomainDef
        {
            Name = context.name.Text,
            NameLocations = { GetRange(context.name) },
            TypeDef = type,
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { properties },
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
            TypeDef = context.ANY() != null ? new OidAnyType { SourceRange = GetRange(context) } : (TypeDef)VisitChildren(context),
            SourceRange = GetRange(context),
        };
    }

    public override TypeDef VisitBlackboxType([NotNull] Interlis24Parser.BlackboxTypeContext context)
    {
        return new BlackboxType
        {
            Kind = context.XML() != null ? BlackboxType.BlackboxTypeKind.Xml : BlackboxType.BlackboxTypeKind.Binary,
            SourceRange = GetRange(context),
        };
    }

    public override TypeDef VisitAlignmentType([NotNull] Interlis24Parser.AlignmentTypeContext context)
    {
        return new TypeRef { Extends = CreateReference<DomainDef>(["INTERLIS", context.GetText()]) };
    }

    public override TypeDef VisitLineType([NotNull] Interlis24Parser.LineTypeContext context)
    {
        var lineForm = context.lineForm() != null ? VisitLineForm(context.lineForm()) : Enumerable.Empty<string>();
        var overlap = context.numeric() != null ? ((Tuple<double, int>)Visit(context.numeric())).Item1 : 0.0;

        if (context.POLYLINE() != null || context.MULTIPOLYLINE() != null)
        {
            return new PolyLineType
            {
                IsMultiGeometry = context.MULTIPOLYLINE() != null,
                IsDirected = context.DIRECTED() != null,
                OverlapTolerance = overlap,
                LineForm = { lineForm },
                VertexType = CreateReference<DomainDef>(context.vertexType),
                SourceRange = GetRange(context),
            };
        }
        else
        {
            return new SurfaceType
            {
                IsMultiGeometry = context.MULTIAREA() != null || context.MULTISURFACE() != null,
                IsCoverage = context.AREA() != null || context.MULTIAREA() != null,
                OverlapTolerance = overlap,
                LineForm = { lineForm },
                VertexType = CreateReference<DomainDef>(context.vertexType),
                SourceRange = GetRange(context),
            };
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
        return new EnumerationAllOfType
        {
            TargetEnumeration = CreateReference<DomainDef>(context.definitionRef()),
            SourceRange = GetRange(context),
        };
    }

    public override EnumerationType VisitEnumerationType([NotNull] Interlis24Parser.EnumerationTypeContext context)
    {
        return new EnumerationType
        {
            Sequencing = (EnumerationType.Sequencings)(context.sequencing?.Type ?? 0),
            Values = { VisitEnumeration(context.enumeration()) },
            SourceRange = GetRange(context),
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

        leaf.DocComments.Add(GetDocComments(context));
        leaf.MetaAttributes.Add(ProcessMetaAttributes(context));
        if (context.enumeration() != null)
        {
            leaf.SubValues.Add(VisitEnumeration(context.enumeration()));
        }

        return root;
    }

    public override List<Tuple<string, string>> VisitMetaComments([NotNull] Interlis24Parser.MetaCommentsContext context)
    {
        return context.metaComment().SelectMany(VisitMetaComment).ToList();
    }

    public override List<Tuple<string, string>> VisitMetaComment([NotNull] Interlis24Parser.MetaCommentContext context)
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
    /// Get a collection of <see cref="Property"/> from the <see cref="Interlis24Parser.PropertiesContext"/>.
    /// </summary>
    private HashSet<Property> VisitProperties(Interlis24Parser.PropertiesContext context, int[] allowedProperties)
    {
        var result = new HashSet<Property>();
        if (context == null) return result;

        var properties = VisitProperties(context);
        foreach (var property in properties)
        {
            if (!result.Add((Property)property.Type))
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
        if (context.@string() != null) return new TextConstant { Value = VisitString(context.@string()) };
        if (context.UNDEFINED() != null) return new UndefinedConstant();

        return (ConstantExpression)VisitChildren(context);
    }

    public override NumericConstant VisitNumericConst([NotNull] Interlis24Parser.NumericConstContext context)
    {
        var (value, precision) = VisitDecConst(context.decConst());
        return new NumericConstant { Value = value, Unit = CreateReference<UnitDef>(context.definitionRef()) };
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

    public override object VisitAttributePathConst([NotNull] Interlis24Parser.AttributePathConstContext context)
    {
        IPathElement pathStart = context.definitionRef() == null ?
            new KeyWordPathElement { Value = KeyWordPathElement.KeyWord.This } :
            new ReferencePathElement { Value = CreateReference<IInterlisDefinition>(context.definitionRef()) };

        return new PathExpression
        {
            Path =
            {
                pathStart,
                new IdentifierPathElement { Value = context.attribute.Text },
            },
        };
    }

    public override object VisitClassConst([NotNull] Interlis24Parser.ClassConstContext context)
    {
        return new PathExpression
        {
            Path =
            {
                new ReferencePathElement { Value = CreateReference<IInterlisDefinition>(context.definitionRef()) }
            },
        };
    }

    public override PathExpression VisitObjectOrAttributePath([NotNull] Interlis24Parser.ObjectOrAttributePathContext context)
    {
        return new PathExpression
        {
            Path = { context.pathEl().SelectMany(VisitPathEl).ToList() },
        };
    }

    public override IEnumerable<IPathElement> VisitPathEl([NotNull] Interlis24Parser.PathElContext context)
    {
        if (context.name != null)
        {
            yield return new IdentifierPathElement { Value = context.name.Text };

            if (context.detail != null)
            {
                if (context.FIRST() != null)
                {
                    yield return new KeyWordPathElement { Value = KeyWordPathElement.KeyWord.First };
                }
                else if (context.LAST() != null)
                {
                    yield return new KeyWordPathElement { Value = KeyWordPathElement.KeyWord.Last };
                }
                else if (context.POS_NUMBER() != null)
                {
                    yield return new IndexerPathElement { Value = int.Parse(context.POS_NUMBER().Symbol.Text) };
                }
                else if (context.IDENTIFIER(1) != null)
                {
                    yield return new IdentifierPathElement { Value = context.IDENTIFIER(1).GetText() };
                }
            }
        }
        else
        {
            yield return new KeyWordPathElement
            {
                Value = (KeyWordPathElement.KeyWord)context.keyword.Type,
            };
        }
    }

    public override object VisitFunctionDef([NotNull] Interlis24Parser.FunctionDefContext context)
    {
        return new FunctionDef
        {
            Name = context.name.Text,
            NameLocations = { GetRange(context.name) },
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            ReturnType = VisitArgumentType(context.returnType),
        };
    }

    public override TypeDef VisitArgumentType([NotNull] Interlis24Parser.ArgumentTypeContext context)
    {
        if (context.attrTypeDef() != null)
        {
            return VisitAttrTypeDef(context.attrTypeDef());
        }
        else if (context.OBJECT() != null || context.OBJECTS() != null)
        {
            return new ObjectType
            {
                SourceRange = GetRange(context),
            };
        }
        else
        {
            // ENUMVAL or ENUMTREEVAL
            return new EnumerationType
            {
                SourceRange = GetRange(context),
            };
        }
    }

    public override object VisitFunctionCall([NotNull] Interlis24Parser.FunctionCallContext context)
    {
        return new FunctionCall
        {
            FunctionDef = CreateReference<FunctionDef>(context.definitionRef()),
            Arguments = { context.argument().Select(VisitArgument) },
        };
    }

    public override IExpression VisitArgument([NotNull] Interlis24Parser.ArgumentContext context)
    {
        if (context.expression() != null)
        {
            return (IExpression)Visit(context.expression());
        }
        else
        {
            return new AllExpression
            {
                Restriction = context.restrictedDefinitionRef() != null ? VisitRestrictedDefinitionRef(context.restrictedDefinitionRef()) : null,
            };
        }
    }
}
