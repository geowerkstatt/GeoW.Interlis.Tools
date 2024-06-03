using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Geowerkstatt.Interlis.Tools.AST;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.CreateAST;

public sealed class Interlis24Visitor : ThrowingInterlis24ParserBaseVisitor<object>
{
    private List<ReferenceToResolve> ReferencestoResolve = new List<ReferenceToResolve>();

    private IAntlrErrorListener<IToken> errorListener;

    /// <summary>
    /// The surrounding scope of the current element.
    /// </summary>
    private Identifier CurrentScope { get; set; } = new Identifier();

    private record ReferenceToResolve
    {
        public required Action<IInterlisDefinition> SetSource;
        public required Identifier Target;
        public IList<Identifier> Restrictions = new List<Identifier>();
    };

    private void ResolveReferences(IContainer<ModelDef> file, ReferenceToResolve reference)
    {
        if (reference.Target.Model != null)
        {
            ResolveModel(file, reference);
        }
    }

    private void ResolveModel(IContainer<ModelDef> file, ReferenceToResolve reference)
    {
        foreach (var item in file.Children)
        {
            if (string.Equals(item.FullyQualifiedName.Model, reference.Target.Model))
            {
                ResolveTopic(item, reference);
                break;
            }
        }
    }

    private void ResolveTopic(IContainer<IInterlisDefinition> model, ReferenceToResolve reference)
    {
        foreach (var item in model.Children)
        {
            if (reference.Target.Topic == null || string.Equals(item.FullyQualifiedName.Topic, reference.Target.Topic))
            {
                if (item is IContainer<IInterlisDefinition> container)
                {
                    if (ResolveClass(container, reference))
                    {
                        return;
                    }
                }
                else if (string.Equals(item.FullyQualifiedName.Class, reference.Target.Class))
                {
                    reference.SetSource(item);
                }
            }
        }
    }

    private bool ResolveClass(IContainer<IInterlisDefinition> topic, ReferenceToResolve reference)
    {
        foreach (var item in topic.Children)
        {
            if (string.Equals(item.FullyQualifiedName.Class, reference.Target.Class))
            {
                reference.SetSource(item);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sets the CurrentScope of the <see cref="Interlis24Visitor"/> to the specified new scope 
    /// and resets the value when disposed.
    /// </summary>
    private sealed class ScopeFrame : IDisposable
    {
        private Identifier previousScope;
        private Interlis24Visitor visitor;
        private bool isDisposed;

        public ScopeFrame(Interlis24Visitor visitor, Identifier newScope)
        {
            this.visitor = visitor;

            previousScope = visitor.CurrentScope;
            visitor.CurrentScope = newScope;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                visitor.CurrentScope = previousScope;
                isDisposed = true;
            }
        }
    }

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

    public override InterlisFile VisitInterlis([NotNull] Interlis24Parser.InterlisContext context)
    {
        var interlisFile = new InterlisFile
        {
            Children = { context.modelDef().Select(VisitModelDef) }
        };

        foreach (var reference in ReferencestoResolve)
        {
            ResolveReferences(interlisFile, reference);
        }

        return interlisFile;
    }

    public override ModelDef VisitModelDef([NotNull] Interlis24Parser.ModelDefContext context)
    {
        using var scopeFrame = new ScopeFrame(this, CurrentScope with { Model = context.name.Text });

        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);

        return new ModelDef
        {
            FullyQualifiedName = CurrentScope,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
            Language = context.language?.Text,
            URI = VisitString(context.uri),
            Version = VisitString(context.modelVersion),
            Children = { context.modelContents().Select(Visit).Cast<IInterlisDefinition>() },
        };
    }

    public override object VisitModelContents([NotNull] Interlis24Parser.ModelContentsContext context)
    {
        return VisitChildren(context);
    }

    public override TopicDef VisitTopicDef([NotNull] Interlis24Parser.TopicDefContext context)
    {
        using var scopeFrame = new ScopeFrame(this, CurrentScope with { Topic = context.name.Text });

        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);

        return new TopicDef
        {
            FullyQualifiedName = CurrentScope,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
            Children = { context.topicContents().Select(Visit).Cast<IInterlisDefinition>() },
        };
    }

    public override object VisitTopicContents([NotNull] Interlis24Parser.TopicContentsContext context)
    {
        return VisitChildren(context);
    }

    public override ClassDef VisitClassDef([NotNull] Interlis24Parser.ClassDefContext context)
    {
        using var scopeFrame = new ScopeFrame(this, CurrentScope with { Class = context.name.Text });

        CheckStartAndEndName(context.endName, context.name.Text, context.endName.Text);

        //Visit(context.classOrStructureDef());

        return new ClassDef
        {
            FullyQualifiedName = CurrentScope,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
        };
    }

    public override AssociationDef VisitAssociationDef([NotNull] Interlis24Parser.AssociationDefContext context)
    {
        using var scopeFrame = new ScopeFrame(this, CurrentScope with { Class = context.name.Text });

        CheckStartAndEndName(context.endName ?? context.Start, context.name?.Text ?? string.Empty, context.endName?.Text ?? string.Empty);

        List<AttributeDef> attributeDefs = context.roleDef().Select(VisitRoleDef).ToList();

        return new AssociationDef
        {
            FullyQualifiedName = CurrentScope,
            RoleDefs = { attributeDefs },
        };
    }

    public override AttributeDef VisitRoleDef([NotNull] Interlis24Parser.RoleDefContext context)
    {
        using var scopeFrame = new ScopeFrame(this, CurrentScope with { LeafElementName = context.name.Text });

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

        var target = new ReferenceType { Cardinality = cardinality };
        ReferencestoResolve.Add(new ReferenceToResolve { SetSource = e => target.Target = e, Target = VisitRestrictedDefinitionRef(context.restrictedDefinitionRef()[0]).Item1 });

        return new AttributeDef
        {
            FullyQualifiedName = CurrentScope,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
            TypeDef = target,
        };
    }

    public override Tuple<Identifier, List<Identifier>> VisitRestrictedDefinitionRef([NotNull] Interlis24Parser.RestrictedDefinitionRefContext context)
    {
        var target = VisitDefinitionRef(context.@ref);
        var restrictions = context._restrictions.Select(VisitDefinitionRef).ToList();

        return Tuple.Create(target, restrictions);
    }

    public override Identifier VisitDefinitionRef([NotNull] Interlis24Parser.DefinitionRefContext context)
    {
        return new Identifier
        {
            Model = context.model?.Text ?? CurrentScope.Model,
            Topic = context.topic?.Text,
            Class = context.name?.Text,
        };
    }

    public override AttributeDef VisitAttributeDef([NotNull] Interlis24Parser.AttributeDefContext context)
    {
        using var scopeFrame = new ScopeFrame(this, CurrentScope with { LeafElementName = context.name.Text });

        return new AttributeDef
        {
            FullyQualifiedName = CurrentScope,
            DocComments = { context.DOC_COMMENT().Select(d => d.GetText()) },
            MetaAttributes = { ProcessMetaAttributes(context, context.metaAttributes()) },
            TypeDef = VisitAttrTypeDef(context.attrTypeDef()),
        };
    }

    public override TypeDef VisitAttrTypeDef([NotNull] Interlis24Parser.AttrTypeDefContext context)
    {
        Cardinality cardinality;
        if (context.MANDATORY == null)
        {
            if (context.OF == null)
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
        else
        {
            cardinality = new Cardinality { Min = 1, Max = 1 };
        }

        return new TypeDef
        {
            FullyQualifiedName = new Identifier(),
            Definition = context.attrType().GetText(),
            Cardinality = cardinality,
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
