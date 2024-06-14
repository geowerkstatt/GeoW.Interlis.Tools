using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Geowerkstatt.Interlis.Tools.AST;
using System.Globalization;
using System.Text;

namespace Geowerkstatt.Interlis.Tools.CreateAST;

public sealed class Interlis24Visitor : ThrowingInterlis24ParserBaseVisitor<object>
{
    private List<UnresolvedReference> ReferencestoResolve = new List<UnresolvedReference>();
    private Scope<IInterlisDefinition> CurrentScope = new Scope<IInterlisDefinition>();

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

    private void SetContentDictionary<T>(IContainer<IInterlisDefinition> container, IInterlisDefinition? parent, IToken token, IEnumerable<T> elements) where T : IInterlisDefinition
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

        foreach (var reference in ReferencestoResolve)
        {
            if (!reference.TryResolve(interlisFile)) {
                ReportError(context.Start, $"Could not resolve {reference}");
            }
        }

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

        using var scopeFrame = CurrentScope.NewFrame(modelDef);
        SetContentDictionary(modelDef, modelDef, context.name, context.modelContents().Select(Visit).Cast<IInterlisDefinition>());
        return modelDef;
    }

    public override object VisitModelContents([NotNull] Interlis24Parser.ModelContentsContext context)
    {
        return VisitChildren(context);
    }

    public override TopicDef VisitTopicDef([NotNull] Interlis24Parser.TopicDefContext context)
    {
        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);

        var topicDef =  new TopicDef
        {
            Name = context.name.Text,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
        };

        if (context.extends != null)
        {
            var extendsRef = VisitTopicRef(context.extends);
            extendsRef.SetSource = e => topicDef.Extends = (TopicDef)e;
            extendsRef.Source = topicDef;
            ReferencestoResolve.Add(extendsRef);
        }

        using var scopeFrame = CurrentScope.NewFrame(topicDef);
        SetContentDictionary(topicDef, topicDef, context.name, context.topicContents().Select(Visit).Cast<IInterlisDefinition>());
        return topicDef;
    }

    public override object VisitTopicContents([NotNull] Interlis24Parser.TopicContentsContext context)
    {
        return VisitChildren(context);
    }

    public override ClassDef VisitClassDef([NotNull] Interlis24Parser.ClassDefContext context)
    {
        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);

        var classDef = new ClassDef
        {
            Name = context.name.Text,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
        };

        if (context.extends != null)
        {
            var extendsRef = VisitDefinitionRef(context.extends);
            extendsRef.SetSource = e => classDef.Extends = (ClassDef)e;
            extendsRef.Source = classDef;
            ReferencestoResolve.Add(extendsRef);
        }

        SetContentDictionary(classDef, classDef, context.name, VisitClassOrStructureDef(context.classOrStructureDef()));

        return classDef;
    }

    public override List<IInterlisDefinition> VisitClassOrStructureDef([NotNull] Interlis24Parser.ClassOrStructureDefContext context)
    {
        var constraints = context.constraintDef().Select(VisitConstraintDef).Cast<IInterlisDefinition>();
        var attributes = context.attributeDef().Select(VisitAttributeDef).Cast<IInterlisDefinition>();
        var parameters = context.parameterDef() == null ? null : VisitParameterDef(context.parameterDef());

        return attributes.Concat(constraints).ToList();
    }

    public override AssociationDef VisitAssociationDef([NotNull] Interlis24Parser.AssociationDefContext context)
    {
        CheckStartAndEndName(context.endName ?? context.Start, context.name?.Text ?? string.Empty, context.endName?.Text ?? string.Empty);

        var roleDefs = context.roleDef().Select(VisitRoleDef).Cast<IInterlisDefinition>();
        var attributeDefs = context.attributeDef().Select(VisitAttributeDef).Cast<IInterlisDefinition>();
        var constraintDefs = context.constraintDef().Select(VisitConstraintDef).Cast<IInterlisDefinition>();

        var name = context.name?.Text ?? string.Concat(roleDefs.Select(r => r.Name));

        var associationDef = new AssociationDef
        {
            Name = name,
            Cardinality = context.cardinality() != null ? VisitCardinality(context.cardinality()) : new Cardinality { Min = 0, Max = Cardinality.UNBOUND },
        };

        SetContentDictionary(associationDef, associationDef, context.Start, roleDefs.Concat(attributeDefs).Concat(constraintDefs));

        return associationDef;
    }

    public override AttributeDef VisitRoleDef([NotNull] Interlis24Parser.RoleDefContext context)
    {
        // Read Cardinality
        Cardinality cardinality;
        var cardinalityContext = context.cardinality();
        var type = (Cardinality.RelationshipType)context.referenceType.Type;
        if (cardinalityContext == null)
        {
            cardinality = type switch
            {
                Cardinality.RelationshipType.Association => new Cardinality { Min = 0, Max = Cardinality.UNBOUND, Type = type },
                Cardinality.RelationshipType.Aggregation => new Cardinality { Min = 0, Max = Cardinality.UNBOUND, Type = type },
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
            var references = new RestrictedRef();

            var referenceTarget = restrictedRef.Item1;
            referenceTarget.SetSource = (e =>
            {
                references.Target = e;
            });
            referenceTarget.Source = CurrentScope.Value;
            ReferencestoResolve.Add(referenceTarget);

            foreach (var item in restrictedRef.Item2)
            {
                item.SetSource = (e => references.Restrictions.Add(e));
                item.Source = CurrentScope.Value;
                ReferencestoResolve.Add(referenceTarget);
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

    public override UnresolvedReference VisitTopicRef([NotNull] Interlis24Parser.TopicRefContext context)
    {
        return new UnresolvedReference
        {
            Target = { new[] { context.model?.Text, context.topic.Text }.WhereNotNull() },
            IsRelative = context.model == null,
        };
    }

    public override UnresolvedReference VisitDefinitionRef([NotNull] Interlis24Parser.DefinitionRefContext context)
    {
        return new UnresolvedReference
        {
            Target = { new[] { context.model?.Text, context.topic?.Text, context.name.Text }.WhereNotNull() },
            IsRelative = context.model == null,
        };
    }

    public override AttributeDef VisitAttributeDef([NotNull] Interlis24Parser.AttributeDefContext context)
    {
        return new AttributeDef
        {
            Name = context.name.Text,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
            TypeDef = VisitAttrTypeDef(context.attrTypeDef()),
        };
    }

    public override ITypeDef VisitAttrTypeDef([NotNull] Interlis24Parser.AttrTypeDefContext context)
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
                    cardinality = new Cardinality { Min = 0, Max = Cardinality.UNBOUND, Ordered = isOrdered };
                }
                else
                {
                    cardinality = VisitCardinality(cardinalityContext) with { Ordered = isOrdered };
                }
            }
        }

        var type = (ITypeDef)VisitAttrType(context.attrType());
        type.Cardinality = cardinality;

        return type;
    }

    public override object VisitAttrType([NotNull] Interlis24Parser.AttrTypeContext context)
    {
        return VisitChildren(context);
    }

    public override object VisitType([NotNull] Interlis24Parser.TypeContext context)
    {
        return VisitChildren(context);
    }

    public override object VisitBaseType([NotNull] Interlis24Parser.BaseTypeContext context)
    {
        return VisitChildren(context);
    }

    public override TextTypeDef VisitTextType([NotNull] Interlis24Parser.TextTypeContext context)
    {
        return new TextTypeDef
        {
            Length = int.Parse(context.maxLength.Text),
        };
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
            return Cardinality.UNBOUND;
        }

        var fromValue = parseCardinalityValue(context.from);
        long? toValue;
        if (context.to == null)
        {
            toValue = fromValue;
            if (fromValue == Cardinality.UNBOUND)
            {
                fromValue = 0;
            }
        }
        else
        {
            toValue = parseCardinalityValue(context.to);
            if (fromValue == Cardinality.UNBOUND)
            {
                ReportError(context.from, $"Invalid cardinality '{context.GetText()}', did you mean '{{0..{context.to.Text}}}'");
                fromValue = 0;
            }

            if (toValue != Cardinality.UNBOUND && fromValue > toValue)
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
}
