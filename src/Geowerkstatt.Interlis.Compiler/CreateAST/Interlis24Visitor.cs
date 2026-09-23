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
public sealed class Interlis24Visitor : LoggingInterlis24ParserBaseVisitor<object?>
{
    private readonly ILogger logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly CommonTokenStream tokenStream;

    /// <param name="loggerFactory">The Factory to create logger instances.</param>
    /// <param name="tokenStream">The <see cref="CommonTokenStream"/> to access hidden tokens.</param>
    public Interlis24Visitor(ILoggerFactory loggerFactory, CommonTokenStream tokenStream)
        : base(loggerFactory)
    {
        logger = loggerFactory.CreateLogger<Interlis24Visitor>();
        this.loggerFactory = loggerFactory;
        this.tokenStream = tokenStream;
    }

    private Scope<IInterlisDefinitionContainer> CurrentScope = new Scope<IInterlisDefinitionContainer>();

    /// <summary>
    /// Report an error at the position of the <paramref name="offendingToken"/>.
    /// </summary>
    /// <param name="offendingToken">The <see cref="IToken"/> that caused the error.</param>
    /// <param name="message">The error message.</param>
    private void ReportError(IToken offendingToken, string message)
    {
        logger.LogError("Compile error at {Range} {Message}.", offendingToken.ToRange(), message);
    }

    /// <summary>
    /// Logs a compile warning for the given <paramref name="offendingToken"/>. Unlike <see cref="ReportError(IToken, string)"/>
    /// this does not make the input invalid; it flags legal but discouraged modelling.
    /// </summary>
    /// <param name="offendingToken">The <see cref="IToken"/> the warning refers to.</param>
    /// <param name="message">The warning message.</param>
    private void ReportWarning(IToken offendingToken, string message)
    {
        logger.LogWarning("Compile warn at {Range} {Message}.", offendingToken.ToRange(), message);
    }

    /// <summary>
    /// Check if a terminal node is valid (not an error node and not a missing token).
    /// </summary>
    private bool IsValidToken(IToken? terminal)
    {
        return terminal != null
            && terminal is not IErrorNode
            && terminal.TokenIndex != -1;
    }

    /// <summary>
    /// Create a new <see cref="Reference{T}"/> from the given <paramref name="referenceContext"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(referenceContext))]
    private Reference<T>? CreateReference<T>(Interlis24Parser.DefinitionRefContext? referenceContext, Func<IReferenceTarget, T?>? mapTarget = null) where T : class, IReferenceTarget
    {
        return referenceContext == null ? null : CreateReference<T>(VisitDefinitionRef(referenceContext), mapTarget);
    }

    /// <summary>
    /// Create a new <see cref="Reference{T}"/> with the given <paramref name="path"/> and register it with the
    /// enclosing container. Every reference standing for a name written in the source goes through here, whatever
    /// its <paramref name="resolution"/>, so <see cref="IInterlisDefinitionContainer.ContainerReferences"/> holds
    /// all of them for navigation and rename.
    /// </summary>
    private Reference<T> CreateReference<T>(IEnumerable<PathSegment> path, Func<IReferenceTarget, T?>? mapTarget = null, ReferenceResolution resolution = ReferenceResolution.Scoped) where T : class, IReferenceTarget
    {
        var reference = new Reference<T>
        {
            Path = { path },
            Source = CurrentScope.Value,
            MapTarget = mapTarget ?? (element => element as T),
            Resolution = resolution,
        };

        CurrentScope.Value?.ContainerReferences.Add(reference);

        return reference;
    }

    /// <summary>
    /// Create a registered <see cref="ReferenceResolution.Member"/> reference for a single name
    /// <paramref name="token"/>: a name the scoped resolver must not look up, because it denotes a member of the
    /// container its context establishes (a <c>BASED ON</c> structure, a basket's topic, the structure a
    /// <c>LOCAL UNIQUE</c> path reaches).
    /// </summary>
    private Reference<T> CreateMemberReference<T>(IToken token) where T : class, IReferenceTarget
    {
        return CreateReference<T>([Segment(token)], resolution: ReferenceResolution.Member);
    }

    /// <summary>The path segment a single name <paramref name="token"/> writes, with its span.</summary>
    private static PathSegment Segment(IToken token) => new() { Name = token.Text, Range = token.ToRange() };

    /// <summary>
    /// The segments of a path the model implies rather than writes (<c>INTERLIS.NOOID</c> behind a <c>NO OID</c>, the
    /// predefined domain behind a type keyword): names without spans, since there is nothing in the source a rename
    /// could rewrite.
    /// </summary>
    private static IEnumerable<PathSegment> ImpliedPath(params string[] names) => names.Select(name => new PathSegment { Name = name });

    /// <summary>
    /// Create a <see cref="ReferenceResolution.Member"/> reference from a <c>metaObjectRef</c>
    /// (<c>[ MetaDataBasketRef '.' ] Metaobject-Name</c>, RefHB 3.10.1). The trailing segment names a declared
    /// meta-object, not a definition, so the scoped resolver leaves the whole path alone.
    /// </summary>
    private Reference<IInterlisDefinition> CreateMetaObjectReference(Interlis24Parser.MetaObjectRefContext context)
    {
        var path = new List<PathSegment>();
        if (context.definitionRef() != null)
        {
            path.AddRange(VisitDefinitionRef(context.definitionRef()));
        }
        // The Metaobject-Name is mandatory in the grammar but can be absent while the reference is still being typed.
        if (IsValidToken(context.metaObjectName))
        {
            path.Add(Segment(context.metaObjectName));
        }

        return CreateReference<IInterlisDefinition>(path, resolution: ReferenceResolution.Member);
    }

    /// <summary>
    /// Report an error if the given <paramref name="startName"/> and <paramref name="endName"/> do not match.
    /// </summary>
    private void CheckStartAndEndName(IToken offendingToken, string? startName, string? endName)
    {
        if (startName != null && endName != null && !string.Equals(startName, endName))
        {
            ReportError(offendingToken, $"Start name '{startName}' and end name '{endName}' do not match");
        }
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
    /// Get the text content of an <c>EXPLANATION</c> (<c>//...//</c>) terminal, stripped of its
    /// delimiters, or <see langword="null"/> if no explanation is present (RefHB 3.2.6).
    /// </summary>
    private static string? GetExplanation(ITerminalNode? explanation)
    {
        var text = explanation?.GetText();
        // The EXPLANATION token is '//' .*? '//', so remove the two leading and two trailing slashes.
        return text?[2..^2];
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

    private void SetContentDictionary<T>(IContainer<T> container, IInterlisDefinitionContainer? parent, IToken token, IEnumerable<T?> elements) where T : class, IInterlisDefinition
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
        double? version = (context.numeric()?.Accept(this) as Tuple<double, int>)?.Item1;

        var interlisFile = new InterlisEnvironment
        {
            Version = version,
            Content = { { InternalModel.Interlis.Name, InternalModel.Interlis } },
        };

        if (version != 2.4)
        {
            var range = context.numeric() is { } versionContext ? versionContext.ToRange() : context.Start.ToRange();
            logger.LogWarning("Unsupported INTERLIS version {Version} at {Range}. Only version 2.4 is supported.", version, range);
        }

        SetContentDictionary(interlisFile, null, context.Start, context.modelDef().Select(VisitModelDef));
        return interlisFile;
    }

    public override ModelDef? VisitModelDef([NotNull] Interlis24Parser.ModelDefContext context)
    {
        if (!IsValidToken(context.name))
        {
            return null;
        }

        CheckStartAndEndName(context.endName, context.name.Text, context.endName?.Text);

        var modelDef = new ModelDef
        {
            Name = context.name.Text,
            NameLocations = { new[] { context.name, context.endName }.WhereNotNull().Select(RangeExtensions.ToRange) },
            SourceRange = context.ToRange(),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Language = context.language?.Text,
            URI = context.uri.WhenNotNull(VisitString),
            Version = context.modelVersion.WhenNotNull(VisitString),
            Xmlns = context.xmlns.WhenNotNull(VisitString),
            Explanation = GetExplanation(context.EXPLANATION()),
            Type =
                context.TYPE() != null ? ModelDef.ModelType.Type :
                context.REFSYSTEM() != null ? ModelDef.ModelType.Refsystem :
                context.SYMBOLOGY() != null ? ModelDef.ModelType.Symbology :
                ModelDef.ModelType.None,
            NoIncrementalTransfer = context.NOINCREMENTALTRANSFER() != null,
            Charset = context.charsetName.WhenNotNull(VisitString),
            TranslationOfVersion = context.translationOfVersion.WhenNotNull(VisitString),
        };

        if (modelDef.URI != null && !Uri.IsWellFormedUriString(modelDef.URI, UriKind.Absolute))
        {
            ReportError(context.uri.Start, $"Model URI '{modelDef.URI}' is not a valid URI");
        }

        using var scopeFrame = CurrentScope.NewFrame(modelDef);

        // Register the TRANSLATION OF link inside the model scope so it is resolved like any other reference
        // (against the sibling models in the InterlisEnvironment, RefHB 3.5.1-10).
        if (context.translationOf != null)
        {
            modelDef.TranslationOf = CreateReference<IInterlisDefinition>(
                [Segment(context.translationOf)],
                mapTarget: element => element as ModelDef,
                resolution: ReferenceResolution.Environment);
        }

        foreach (var import in context.modelImport())
        {
            // The imported model name can still be missing while typing (e.g. 'IMPORTS' or a trailing ',').
            if (!IsValidToken(import.name))
            {
                continue;
            }

            var importModelName = import.name.Text;
            if (!modelDef.Imports.TryAdd(importModelName, (import.UNQUALIFIED() != null, CreateReference<ModelDef>([Segment(import.name)], resolution: ReferenceResolution.Environment))))
            {
                ReportError(import.name, $"Duplicate import {importModelName}");
            }
        }

        // Add default INTERLIS import
        modelDef.Imports.TryAdd(InternalModel.Interlis.Name, (false, CreateReference<ModelDef>(ImpliedPath(InternalModel.Interlis.Name), resolution: ReferenceResolution.Environment)));

        var elements = context
            .modelContents()
            .SelectMany(c =>
            {
                // A child that could not be built (mid-typing input) contributes nothing.
                var result = Visit(c);
                if (result is IEnumerable collection)
                {
                    return collection.Cast<IInterlisDefinition>();
                }

                return result is IInterlisDefinition definition ? [definition] : Enumerable.Empty<IInterlisDefinition>();
            });

        SetContentDictionary(modelDef, modelDef, context.name, elements);
        return modelDef;
    }

    public override object? VisitModelContents([NotNull] Interlis24Parser.ModelContentsContext context)
    {
        return VisitChildrenBase(context);
    }

    public override TopicDef? VisitTopicDef([NotNull] Interlis24Parser.TopicDefContext context)
    {
        // The topic name is mandatory but may still be missing while the definition is being typed. Without it there
        // is nothing to build; the enclosing SetContentDictionary drops the null (mirrors VisitModelDef).
        if (!IsValidToken(context.name))
        {
            return null;
        }

        CheckStartAndEndName(context.endName, context.name.Text, context.endName?.Text);
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.FINAL]);

        var topicDef = new TopicDef
        {
            Name = context.name.Text,
            NameLocations = { new[] { context.name, context.endName }.WhereNotNull().Select(RangeExtensions.ToRange) },
            SourceRange = context.ToRange(),
            IsView = context.VIEW() != null,
            Extends = CreateReference<TopicDef>(context.extends),
            OidType = CreateReference<DomainDef>(context.oid),
            BasketOidType = CreateReference<DomainDef>(context.basketOid),
            DependsOn = { context._dependsOn.Select(r => CreateReference<TopicDef>(r)).WhereNotNull() },
            DeferredGenerics = { context._generics.Select(r => CreateReference<DomainDef>(r)).WhereNotNull() },
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { properties },
        };

        using var scopeFrame = CurrentScope.NewFrame(topicDef);

        var topicContents = context.topicContents();

        var elements = topicContents
            .Where(c => c.constraintsDef() == null)
            .SelectMany(c =>
            {
                // A child that could not be built (mid-typing input) contributes nothing.
                var result = Visit(c);
                if (result is IEnumerable collection)
                {
                    return collection.Cast<IInterlisDefinition>();
                }

                return result is IInterlisDefinition definition ? [definition] : Enumerable.Empty<IInterlisDefinition>();
            });

        // CONSTRAINTS OF blocks (RefHB 3.12-41) attach constraints to a viewable but have no source name; they are
        // given a synthesized, topic-unique name (see BuildConstraintsBlocks) and stored in content like any other
        // definition.
        var constraintBlocks = BuildConstraintsBlocks(topicContents.Select(c => c.constraintsDef()).WhereNotNull());

        SetContentDictionary(topicDef, topicDef, context.name, elements.Concat<IInterlisDefinition>(constraintBlocks));
        return topicDef;
    }

    public override object? VisitTopicContents([NotNull] Interlis24Parser.TopicContentsContext context)
    {
        return VisitChildrenBase(context);
    }

    public override ClassDef? VisitClassDef([NotNull] Interlis24Parser.ClassDefContext context)
    {
        // The class/structure name is mandatory but may still be missing while typing; without it there is nothing to
        // build (mirrors VisitModelDef). The enclosing SetContentDictionary drops the null.
        if (!IsValidToken(context.name))
        {
            return null;
        }

        CheckStartAndEndName(context.endName, context.name.Text, context.endName?.Text);
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL]);

        var classDef = new ClassDef
        {
            Name = context.name.Text,
            NameLocations = { new[] { context.name, context.endName }.WhereNotNull().Select(RangeExtensions.ToRange) },
            SourceRange = context.ToRange(),
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
            classDef.OidType = CreateReference<DomainDef>(ImpliedPath("INTERLIS", "NOOID"));
        }

        using var scopeFrame = CurrentScope.NewFrame(classDef);

        var attributeDefs = context.attributeDef().Select(VisitAttributeDef).Cast<IInterlisDefinition>();
        var parameterDefs = context.parameterDef().Select(VisitParameterDef).Cast<IInterlisDefinition>();

        SetContentDictionary(classDef, classDef, context.name, attributeDefs.Concat(parameterDefs));
        classDef.Constraints.AddRange(BuildConstraints(context.constraintDef()));

        return classDef;
    }

    public override AssociationDef VisitAssociationDef([NotNull] Interlis24Parser.AssociationDefContext context)
    {
        CheckStartAndEndName(context.endName ?? context.Start, context.name?.Text ?? string.Empty, context.endName?.Text ?? string.Empty);
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.OID]);

        var name = context.name?.Text ?? string.Concat(context.roleDef().Select(r => r.name.Text));

        var associationDef = new AssociationDef
        {
            Name = name,
            NameLocations = { new [] { context.name, context.endName }.WhereNotNull().Select(RangeExtensions.ToRange) },
            SourceRange = context.ToRange(),
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
            associationDef.OidType = CreateReference<DomainDef>(ImpliedPath("INTERLIS", "NOOID"));
        }

        using var scopeFrame = CurrentScope.NewFrame(associationDef);

        // RefHB 3.7.1: a derived association is DERIVED FROM a base viewable, addressable within the association
        // under its base name — just like a view's formation base (RefHB 3.15). Build it in the association's scope
        // (so its reference resolves from here) and register it in Content, so the base name shares the
        // Bestandteilnamen namespace with roles/attributes and the role derivations can resolve their path head.
        associationDef.DerivedFrom = context.renamedViewableRef() == null ? null : VisitRenamedViewableRef(context.renamedViewableRef());

        var roleDefs = context.roleDef().Select(VisitRoleDef).Cast<IInterlisDefinition>();
        var attributeDefs = context.attributeDef().Select(VisitAttributeDef).Cast<IInterlisDefinition>();
        IEnumerable<IInterlisDefinition> derivedBase = associationDef.DerivedFrom == null ? [] : [associationDef.DerivedFrom];

        SetContentDictionary(associationDef, associationDef, context.Start, derivedBase.Concat(roleDefs).Concat(attributeDefs));
        associationDef.Constraints.AddRange(BuildConstraints(context.constraintDef()));

        return associationDef;
    }

    public override ViewDef? VisitViewDef([NotNull] Interlis24Parser.ViewDefContext context)
    {
        // The view name is mandatory but may still be missing while typing; without it there is nothing to build
        // (mirrors VisitModelDef). The enclosing SetContentDictionary drops the null.
        if (!IsValidToken(context.name))
        {
            return null;
        }

        CheckStartAndEndName(context.endName, context.name.Text, context.endName?.Text);
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.TRANSIENT]);

        var viewDef = new ViewDef
        {
            Name = context.name.Text,
            NameLocations = { new[] { context.name, context.endName }.WhereNotNull().Select(RangeExtensions.ToRange) },
            SourceRange = context.ToRange(),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { properties },
            Extends = CreateReference<ViewDef>(context.extends),
        };

        using var scopeFrame = CurrentScope.NewFrame(viewDef);

        viewDef.Formation = context.formationDef() == null ? null : VisitFormationDef(context.formationDef());
        viewDef.Selections.AddRange(context.selection().Select(s => Visit(s.expression()) as IExpression).WhereNotNull());
        viewDef.BaseExtensions.AddRange(context.baseExtensionDef().Select(VisitBaseExtensionDef));

        var attributes = new List<IInterlisDefinition>();
        foreach (var viewAttribute in context.viewAttributes()?.viewAttribute() ?? [])
        {
            var result = Visit(viewAttribute);
            if (result is IInterlisDefinition definition)
            {
                attributes.Add(definition);
            }
            else if (result is Reference<BaseView> allOfBase)
            {
                viewDef.AllOfBases.Add(allOfBase);
            }
        }

        // RefHB 3.15 / 3.5.4: each base viewable of the formation is addressable within the view under its base
        // name, which shares the Bestandteilnamen namespace with attributes and roles. Register the base views as
        // members of the view (before the attributes) so a clash with an attribute — or a duplicate base name — is
        // reported by SetContentDictionary like any other duplicate, and so tooling can resolve the base name.
        IEnumerable<IInterlisDefinition> baseViews = viewDef.Formation switch
        {
            ProjectionView projection => [projection.Source],
            JoinView join => join.Sources.Select(source => source.Viewable),
            UnionView union => union.Sources,
            AggregationView aggregation => [aggregation.Source],
            InspectionView inspection => [inspection.Source],
            _ => [],
        };
        SetContentDictionary(viewDef, viewDef, context.name, baseViews.Concat(attributes));
        viewDef.Constraints.AddRange(BuildConstraints(context.constraintDef()));

        return viewDef;
    }

    public override ViewFormation VisitFormationDef([NotNull] Interlis24Parser.FormationDefContext context)
    {
        // Dispatch explicitly: formationDef ends with ';', so VisitChildren would return the terminal's result.
        // 'inspection' is shared with the expression grammar (factor), so build it explicitly here instead of
        // overriding VisitInspection (which would clash with its use as an expression).
        if (context.projection() != null) return VisitProjection(context.projection());
        if (context.join() != null) return VisitJoin(context.join());
        if (context.union() != null) return VisitUnion(context.union());
        if (context.aggregation() != null) return VisitAggregation(context.aggregation());
        return BuildInspectionView(context.inspection());
    }

    public override ProjectionView VisitProjection([NotNull] Interlis24Parser.ProjectionContext context)
    {
        return new ProjectionView { Source = VisitRenamedViewableRef(context.renamedViewableRef()) };
    }

    public override JoinView VisitJoin([NotNull] Interlis24Parser.JoinContext context)
    {
        var join = new JoinView();
        // The base source (RefHB 3.15-32) is a plain viewable and can not be marked '(OR NULL)'; only the appended
        // sources may be. The grammar enforces this, so the base source never carries OrNull.
        join.Sources.Add(new JoinViewSource { Viewable = VisitRenamedViewableRef(context.renamedViewableRef()) });
        join.Sources.AddRange(context.joinSource().Select(VisitJoinSource));
        return join;
    }

    public override JoinViewSource VisitJoinSource([NotNull] Interlis24Parser.JoinSourceContext context)
    {
        return new JoinViewSource
        {
            Viewable = VisitRenamedViewableRef(context.renamedViewableRef()),
            OrNull = context.NULL() != null,
        };
    }

    public override UnionView VisitUnion([NotNull] Interlis24Parser.UnionContext context)
    {
        var union = new UnionView();
        union.Sources.AddRange(context.renamedViewableRef().Select(VisitRenamedViewableRef));
        return union;
    }

    public override AggregationView VisitAggregation([NotNull] Interlis24Parser.AggregationContext context)
    {
        var aggregation = new AggregationView
        {
            Source = VisitRenamedViewableRef(context.renamedViewableRef()),
            All = context.ALL() != null,
        };
        if (context.uniqueEl() != null)
        {
            aggregation.UniqueBy.AddRange(context.uniqueEl().objectOrAttributePath().Select(VisitObjectOrAttributePath));
        }
        return aggregation;
    }

    private InspectionView BuildInspectionView(Interlis24Parser.InspectionContext context)
    {
        var inspection = new InspectionView
        {
            IsArea = context.AREA() != null,
            Source = VisitRenamedViewableRef(context.renamedViewableRef()),
            // An object path: each step is a member of the previous step's structure, walked by the path resolver.
            Path = CreateReference<AttributeDef>(context.IDENTIFIER().Select(identifier => identifier.Symbol).Where(IsValidToken).Select(Segment), resolution: ReferenceResolution.ObjectPath),
        };

        return inspection;
    }

    /// <summary>
    /// A <c>ViewableRef</c> (RefHB 3.15-40) may only denote a viewable — a class, structure, association or
    /// view — unlike a role/reference target (<c>RestrictedClassOrAssRef</c>, RefHB 3.6.1-16), which excludes
    /// views. Restricting the reference to viewables also means a same-named attribute in scope (e.g. a view's
    /// own attribute) is never a resolution candidate.
    /// </summary>
    private static IInterlisDefinition? AcceptViewable(IReferenceTarget definition) => definition switch
    {
        ClassDef c => c, // covers both classes and structures
        AssociationDef a => a,
        ViewDef v => v,
        _ => null,
    };

    /// <summary>
    /// Builds the <see cref="BaseView"/> for a <c>renamedViewableRef</c> (<c>[ Base '~' ] ViewableRef</c>, RefHB 3.15).
    /// The base name is the explicit local alias or the referenced viewable's own name, and the name location is the
    /// alias token (or the viewable reference when the alias is implicit) so tooling can locate the base-name
    /// declaration. The single viewable reference is registered for resolution via <see cref="CreateReference"/>.
    /// </summary>
    public override BaseView VisitRenamedViewableRef(Interlis24Parser.RenamedViewableRefContext? context)
    {
        // Callers (projection/join/union/aggregation/inspection/base-extension) pass a renamedViewableRef that can be
        // absent while typing (e.g. just 'PROJECTION'); return an empty base view so the formation can still be built.
        if (context == null)
        {
            return new BaseView { Name = string.Empty, Viewable = null, IsRenamed = false };
        }

        var viewable = CreateReference<IInterlisDefinition>(context.definitionRef(), AcceptViewable);

        var baseView = new BaseView
        {
            Name = context.@base?.Text ?? viewable?.Path.LastOrDefault()?.Name ?? string.Empty,
            SourceRange = context.ToRange(),
            Viewable = viewable,
            IsRenamed = context.@base != null,
        };

        // An implicit base name is declared by the viewable reference itself: that token is where a rename of the
        // base has to write, and where go-to-definition on a use of the base name lands.
        if (context.@base is { } baseToken)
        {
            baseView.NameLocations.Add(baseToken.ToRange());
        }
        else if (viewable?.Path.LastOrDefault()?.Range is { } viewableRange)
        {
            baseView.NameLocations.Add(viewableRange);
        }

        return baseView;
    }

    public override BaseExtension VisitBaseExtensionDef([NotNull] Interlis24Parser.BaseExtensionDefContext context)
    {
        var baseExtension = new BaseExtension { Base = context.@base?.Text ?? string.Empty };
        baseExtension.ExtendedBy.AddRange(context.renamedViewableRef().Select(VisitRenamedViewableRef));
        return baseExtension;
    }

    public override object? VisitAllOfViewAttribute([NotNull] Interlis24Parser.AllOfViewAttributeContext context)
    {
        // The base name is mandatory but may still be missing while typing; VisitViewDef only keeps reference
        // results, so returning null here is silently dropped.
        return IsValidToken(context.@base) ? CreateReference<BaseView>([Segment(context.@base)]) : null;
    }

    public override object? VisitDefinedViewAttribute([NotNull] Interlis24Parser.DefinedViewAttributeContext context)
    {
        return context.attributeDef() is { } attributeDef ? VisitAttributeDef(attributeDef) : null;
    }

    public override object VisitDerivedViewAttribute([NotNull] Interlis24Parser.DerivedViewAttributeContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.TRANSIENT]);
        return new AttributeDef
        {
            Name = context.attribute.Text,
            NameLocations = { context.attribute.ToRange() },
            SourceRange = context.ToRange(),
            TypeDef = UndefinedType.Instance,
            Properties = { properties },
            Values = { VisitFactor(context.factor()) },
        };
    }

    public override GraphicDef? VisitGraphicDef([NotNull] Interlis24Parser.GraphicDefContext context)
    {
        // The graphic name is mandatory but may still be missing while typing (e.g. just 'GRAPHIC'); without it there
        // is nothing to build (mirrors VisitModelDef). The enclosing SetContentDictionary drops the null.
        if (!IsValidToken(context.name))
        {
            return null;
        }

        CheckStartAndEndName(context.endName, context.name.Text, context.endName?.Text);
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.FINAL]);

        var graphicDef = new GraphicDef
        {
            Name = context.name.Text,
            NameLocations = { new[] { context.name, context.endName }.WhereNotNull().Select(RangeExtensions.ToRange) },
            SourceRange = context.ToRange(),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { properties },
            Extends = CreateReference<GraphicDef>(context.extends),
            BasedOn = CreateReference<IInterlisDefinition>(context.basedOn),
            Selections = { context.selection().Select(s => Visit(s.expression()) as IExpression).WhereNotNull() },
        };

        graphicDef.DrawingRules.AddRange(context.drawingRule().Select(VisitDrawingRule));
        return graphicDef;
    }

    public override DrawingRule VisitDrawingRule([NotNull] Interlis24Parser.DrawingRuleContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL]);
        var rule = new DrawingRule
        {
            Name = context.name.Text,
            Sign = CreateReference<IInterlisDefinition>(context.sign),
            Properties = { properties },
            SourceRange = context.ToRange(),
        };
        rule.Assignments.AddRange(context.condSignParamAssignment().Select(VisitCondSignParamAssignment));
        return rule;
    }

    public override CondSignParamAssignment VisitCondSignParamAssignment([NotNull] Interlis24Parser.CondSignParamAssignmentContext context)
    {
        var conditional = new CondSignParamAssignment
        {
            Where = context.expression() == null ? null : Visit(context.expression()) as IExpression,
        };
        conditional.Assignments.AddRange(context.signParamAssignment().Select(VisitSignParamAssignment));
        return conditional;
    }

    public override SignParamAssignment VisitSignParamAssignment([NotNull] Interlis24Parser.SignParamAssignmentContext context)
    {
        // The parameter name and the assigned value can both still be unwritten while typing (e.g. '( )' or 'p :=').
        var assignment = new SignParamAssignment { ParameterName = context.IDENTIFIER()?.GetText() ?? string.Empty };
        if (context.metaObjectRef() != null)
        {
            assignment.MetaObject = CreateMetaObjectReference(context.metaObjectRef());
        }
        else if (context.ACCORDING() != null)
        {
            assignment.According = VisitObjectOrAttributePath(context.objectOrAttributePath());
            assignment.EnumAssignments.AddRange(context.enumAssignment().Select(VisitEnumAssignment));
        }
        else if (context.factor() != null)
        {
            assignment.Value = VisitFactor(context.factor());
        }
        return assignment;
    }

    public override EnumAssignment VisitEnumAssignment([NotNull] Interlis24Parser.EnumAssignmentContext context)
    {
        var assignment = new EnumAssignment();
        if (context.metaObjectRef() != null)
        {
            assignment.MetaObject = CreateMetaObjectReference(context.metaObjectRef());
        }
        else if (context.constant() != null)
        {
            assignment.Value = VisitConstant(context.constant());
        }

        // 'WHEN IN enumRange' — the range (or its values) can still be unwritten while typing.
        var range = context.enumRange()?.enumerationConst();
        if (range is { Length: > 0 })
        {
            assignment.RangeFrom = VisitEnumerationConst(range[0]);
            if (range.Length > 1)
            {
                assignment.RangeTo = VisitEnumerationConst(range[1]);
            }
        }
        return assignment;
    }

    public override AttributeDef VisitRoleDef([NotNull] Interlis24Parser.RoleDefContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.HIDING, Interlis24Parser.ORDERED, Interlis24Parser.EXTERNAL]);

        // Read Cardinality
        Cardinality cardinality;
        var cardinalityContext = context.cardinality();
        var type = (RoleType.RelationshipType)context.referenceType.Type;
        if (cardinalityContext == null)
        {
            cardinality = type switch
            {
                RoleType.RelationshipType.Association => new Cardinality { Min = 0, Max = Cardinality.Unbound },
                RoleType.RelationshipType.Aggregation => new Cardinality { Min = 0, Max = Cardinality.Unbound },
                RoleType.RelationshipType.Composition => new Cardinality { Min = 0, Max = 1 },
                _ => throw new UnexpectedNodeException(context.referenceType)
            };
        }
        else
        {
            cardinality = VisitCardinality(context.cardinality());
        }

        if (type == RoleType.RelationshipType.Composition && cardinality.Max > 1)
        {
            ReportError(context.referenceType, "Composition roles cannot have a maximum cardinality greater than 1");
        }

        // RefHB 3.7.4: ORDERED declares the role's link set ordered. Orderedness lives on the population
        // cardinality — the same home it has for a LIST's element cardinality — not in the property set.
        if (properties.Remove(Property.Ordered))
        {
            cardinality = cardinality with { Ordered = true };
        }

        // Read References
        var target = new RoleType { Cardinality = cardinality, Relationship = type };
        foreach (var restrictedRef in context.restrictedDefinitionRef().Select(VisitRestrictedDefinitionRef))
        {
            target.Targets.Add(restrictedRef);
        }

        var roleDef = new AttributeDef
        {
            Name = context.name.Text,
            NameLocations = { context.name.ToRange() },
            SourceRange = context.ToRange(),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            TypeDef = target,
            Properties = { properties },
        };

        // Derived associations may assign the role a value with ':=' (RefHB 3.7.1); reuse AttributeDef.Values.
        if (context.role != null)
        {
            roleDef.Values.Add(VisitFactor(context.role));
        }

        return roleDef;
    }

    public override object VisitReferenceAttr([NotNull] Interlis24Parser.ReferenceAttrContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.EXTERNAL]);

        // The reference target (RestrictedClassOrAssRef) is mandatory but can be missing after 'REFERENCE TO' while
        // typing; fall back to the undefined type (mirrors the fallback in VisitAttributeDef).
        var targetRef = context.restrictedDefinitionRef();
        if (targetRef == null)
        {
            return UndefinedType.Instance;
        }

        return new ReferenceType
        {
            Target = VisitRestrictedDefinitionRef(targetRef),
            Properties = { properties },
            SourceRange = context.ToRange(),
        };
    }

    public override RestrictedRef VisitRestrictedDefinitionRef([NotNull] Interlis24Parser.RestrictedDefinitionRefContext context)
    {
        // RestrictedDefinitionRef only accepts InterlisDefinitions of certain types
        Func<IReferenceTarget, IInterlisDefinition?> acceptTypes = interlisDef => interlisDef switch
        {
            ClassDef c => c,
            AssociationDef a => a,
            DomainDef d => d, // Domains cannot be restricted, but are accepted because of an ambiguity in 'attrType' that can only be resolved when the target type of the reference is known.
            _ => null,
        };

        // The target is an explicit reference or an ANYCLASS / ANYSTRUCTURE placeholder. The merged grammar rule allows
        // any of them in every position; the type checker enforces which kind each context permits (RefHB 3.6.1-13/-15/-17).
        RestrictedRef.RefTarget value =
            context.ANYCLASS() != null ? RestrictedRef.AnyKind.Class :
            context.ANYSTRUCTURE() != null ? RestrictedRef.AnyKind.Structure :
            new RestrictedRef.DefinitionRef { Reference = CreateReference(context.@ref, acceptTypes)! };

        return new RestrictedRef
        {
            Value = value,
            Restrictions = { context._restrictions.Select(r => CreateReference(r, acceptTypes)).WhereNotNull() },
        };
    }

    public override IEnumerable<PathSegment> VisitDefinitionRef([NotNull] Interlis24Parser.DefinitionRefContext context)
    {
        return new[] { context.model, context.topic, context.name }.WhereNotNull().Select(Segment);
    }

    public override AttributeDef? VisitAttributeDef([NotNull] Interlis24Parser.AttributeDefContext context)
    {
        // The attribute name is mandatory but is not the rule's first token (a CONTINUOUS/SUBDIVISION prefix may
        // precede it), so it can be missing while typing. Without a name there is nothing to build; callers drop the
        // null via SetContentDictionary / the 'is IInterlisDefinition' filter (mirrors VisitModelDef).
        if (!IsValidToken(context.name))
        {
            return null;
        }

        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL, Interlis24Parser.TRANSIENT]);

        return new AttributeDef
        {
            Name = context.name.Text,
            NameLocations = { context.name.ToRange() },
            SourceRange = context.ToRange(),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            TypeDef = context.attrTypeDef() == null ? UndefinedType.Instance : VisitAttrTypeDef(context.attrTypeDef()),
            Properties = { properties },
            Subdivision = context.SUBDIVISION() == null ? AttributeDef.SubdivisionKind.None
                : context.CONTINUOUS() != null ? AttributeDef.SubdivisionKind.ContinuousSubdivision
                : AttributeDef.SubdivisionKind.Subdivision,
            Values = { context.factor().Select(VisitFactor) },
        };
    }

    public override ParameterDef VisitParameterDef([NotNull] Interlis24Parser.ParameterDefContext context)
    {
        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.EXTENDED, Interlis24Parser.FINAL]);

        var parameterDef = new ParameterDef
        {
            Name = context.parameter.Text,
            NameLocations = { context.parameter.ToRange() },
            SourceRange = context.ToRange(),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { properties },
        };

        if (context.attrTypeDef() != null)
        {
            parameterDef.TypeDef = VisitAttrTypeDef(context.attrTypeDef());
        }
        else
        {
            // METAOBJECT [OF metaObject] (RefHB 3.10.2.2)
            parameterDef.IsMetaObject = true;
            parameterDef.MetaObject = CreateReference<ClassDef>(context.metaObject);
        }

        return parameterDef;
    }

    public override List<ParameterDef> VisitRunTimeParameterDef([NotNull] Interlis24Parser.RunTimeParameterDefContext context)
    {
        // PARAMETER (name ':' attrTypeDef ';')* — runtime parameters (RefHB 3.11). Each name pairs with the
        // attrTypeDef at the same index. Reuses ParameterDef (name + type).
        var names = context.IDENTIFIER();
        var types = context.attrTypeDef();
        var result = new List<ParameterDef>();
        for (var i = 0; i < names.Length; i++)
        {
            result.Add(new ParameterDef
            {
                Name = names[i].GetText(),
                NameLocations = { names[i].Symbol.ToRange() },
                SourceRange = names[i].Symbol.ToRange(types[i].Stop),
                TypeDef = VisitAttrTypeDef(types[i]),
            });
        }

        return result;
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
                var isOrdered = context.LIST() != null;
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

        // attrType can be missing or unparseable after a syntax error (e.g. 'MANDATORY' with no
        // following type); fall back to an (inherited) type reference instead of dereferencing null.
        var type = (context.attrType() != null ? VisitAttrType(context.attrType()) : null) ?? new TypeRef();
        type.Cardinality = cardinality;

        return type;
    }

    public override TypeDef? VisitAttrType([NotNull] Interlis24Parser.AttrTypeContext context)
    {
        var restrictedDefinitonRef = context.restrictedDefinitionRef();
        if (restrictedDefinitonRef != null)
        {
            // A merged domain / structure / class reference (RefHB 3.6.1): its kind is not known until the target is
            // resolved, so build a provisional UnresolvedNamedType that the reference resolver rewrites into a TypeRef
            // (domain alias) or a contained-substructure ObjectType.
            return new UnresolvedNamedType
            {
                Target = VisitRestrictedDefinitionRef(restrictedDefinitonRef),
                SourceRange = context.ToRange(),
            };
        }
        else
        {
            return VisitChildrenBase(context) as TypeDef;
        }
    }

    public override object? VisitType([NotNull] Interlis24Parser.TypeContext context)
    {
        return VisitChildrenBase(context);
    }

    public override object? VisitBaseType([NotNull] Interlis24Parser.BaseTypeContext context)
    {
        return VisitChildrenBase(context);
    }

    public override TypeDef VisitTextType([NotNull] Interlis24Parser.TextTypeContext context)
    {
        if (context.TEXT() != null || context.MTEXT() != null)
        {
            return new TextType
            {
                Length = IsValidToken(context.maxLength) ? int.Parse(context.maxLength.Text) : null,
                IsMText = context.MTEXT() != null,
                SourceRange = context.ToRange(),
            };
        }
        else
        {
            return new TypeRef { Extends = CreateReference<DomainDef>(ImpliedPath("INTERLIS", context.GetText())), SourceRange = context.ToRange(), };
        }
    }

    public override NumericType VisitNumericType([NotNull] Interlis24Parser.NumericTypeContext context)
    {
        NumericType numericTypeDef;

        if (context.min is { exception: null } && context.max is { exception: null }
            && Visit(context.min) is Tuple<double, int>(var minValue, var minPrecision)
            && Visit(context.max) is Tuple<double, int>(var maxValue, var maxPrecision))
        {

            // RefHB 3.8.5-3: the Stellenzahl (digit count) of the minimum and the maximum must match. In mantissa
            // notation the digits of the mantissa count, so 0.1E7 .. 0.1000E10 is invalid although a scaling could
            // compensate the difference.
            if (minPrecision != maxPrecision)
            {
                ReportError(context.Start, $"Number minimum and maximum must have the same precision but minimum has precision <{Math.Pow(10, minPrecision)}> and maximum has precision <{Math.Pow(10, maxPrecision)}>");
            }

            // RefHB 3.8.5-3: a float range must give both bounds in mantissa (exponential) notation; a fixed-point
            // range must give neither. Mixing the two notations is invalid.
            var minIsMantissa = context.min is Interlis24Parser.ExpNumberContext;
            var maxIsMantissa = context.max is Interlis24Parser.ExpNumberContext;
            if (minIsMantissa != maxIsMantissa)
            {
                ReportError(context.Start, "Number minimum and maximum must both use mantissa (exponential) notation or both use decimal notation");
            }

            if (minValue > maxValue)
            {
                ReportError(context.Start, $"Number minimum <{minValue}> must be smaller than maximum <{maxValue}>");
                (minValue, maxValue) = (maxValue, minValue);
            }

            // Check if it might be ok to represent the values as a double: each bound must be distinguishable from
            // its double neighbours at the range's precision. In mantissa notation the step scales with the bound's
            // own magnitude (the Stellenzahl counts significant digits at the bound's scaling); in decimal notation
            // it is the uniform 10^precision.
            double StepAt(double value) => minIsMantissa && value != 0
                ? Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) + 1 + minPrecision)
                : Math.Pow(10, minPrecision);
            if ((maxValue - Math.BitDecrement(maxValue)) >= StepAt(maxValue) || (Math.BitIncrement(minValue) - minValue) >= StepAt(minValue))
            {
                ReportError(context.Start, $"The given range <{minValue} .. {maxValue}> with a precision of <{Math.Pow(10, minPrecision)}> cannot be represented by a double precision floating point number");
            }

            // The minimum bound decides the notation of the range (a mixed range is already reported above).
            numericTypeDef = minIsMantissa
                ? new FloatType { MantissaLength = -minPrecision, SourceRange = context.ToRange() }
                : new DecimalType { Precision = minPrecision, SourceRange = context.ToRange() };
            numericTypeDef.Min = minValue;
            numericTypeDef.Max = maxValue;
        }
        else
        {
            // NUMERIC, or a range still incomplete while typing: no bounds and no declared notation.
            numericTypeDef = new NumericType { SourceRange = context.ToRange() };
        }

        numericTypeDef.Circular = context.CIRCULAR() != null;
        numericTypeDef.Unit = CreateReference<UnitDef>(context.unit);
        numericTypeDef.Orientation =
            context.CLOCKWISE() != null ? NumericType.AngleOrientation.Clockwise :
            context.COUNTERCLOCKWISE() != null ? NumericType.AngleOrientation.CounterClockwise :
            NumericType.AngleOrientation.None;
        numericTypeDef.RefSystem = context.refSys() == null ? null : VisitRefSys(context.refSys());

        return numericTypeDef;
    }

    public override RefSys VisitRefSys([NotNull] Interlis24Parser.RefSysContext context)
    {
        // The axis is an optional POS_NUMBER; a missing/synthetic token would fail int.Parse, so only parse a real one.
        var axis = IsValidToken(context.axis) ? int.Parse(context.axis.Text) : (int?)null;

        if (context.coord != null)
        {
            // < coord [axis] >
            return new RefSys { Value = new RefSys.CoordDomainRef { Domain = CreateReference<DomainDef>(context.coord) }, Axis = axis };
        }

        // { metaObjectRef [axis] } — the basket prefix is a scoped reference (resolved like any definition
        // reference); the meta-object name is a declared name, not a definition (RefHB 3.10.1-2), so it is a
        // member reference the basket link writes. While either form is still being typed (a lone '<' or '{'),
        // neither form's content exists yet and the referenced system stays null.
        var metaObjectRef = context.metaObjectRef();
        return new RefSys
        {
            Value = metaObjectRef == null ? null : new RefSys.MetaObjectRef
            {
                Basket = metaObjectRef.definitionRef() == null ? null : CreateReference<MetaDataBasketDef>(metaObjectRef.definitionRef()),
                MetaObject = IsValidToken(metaObjectRef.metaObjectName)
                    ? CreateMemberReference<MetaObjectDeclaration>(metaObjectRef.metaObjectName)
                    : null,
            },
            Axis = axis,
        };
    }

    public override FormattedType VisitFormattedType([NotNull] Interlis24Parser.FormattedTypeContext context)
    {
        return new FormattedType
        {
            Min = context.min == null ? null : VisitString(context.min),
            Max = context.max == null ? null : VisitString(context.max),
            BasedOn = CreateReference<ClassDef>(context.basedOn),
            FormatBaseType = CreateReference<DomainDef>(context.domainRef),
            Format = context.formatDef() == null ? null : VisitFormatDef(context.formatDef()),
            SourceRange = context.ToRange(),
        };
    }

    public override FormatDef VisitFormatDef([NotNull] Interlis24Parser.FormatDefContext context)
    {
        var format = new FormatDef { Inheritance = context.INHERITANCE() != null };

        // Walk the children in source order to preserve the separator / base-attribute sequence.
        // context.children is null when the '(' was typed but nothing inside it yet (mirrors VisitString).
        foreach (var child in context.children ?? [])
        {
            switch (child)
            {
                case Interlis24Parser.StringContext separator:
                    format.Components.Add(new FormatSeparator { Value = VisitString(separator) });
                    break;
                case Interlis24Parser.BaseAttrRefContext baseAttrRef:
                    format.Components.Add(VisitBaseAttrRef(baseAttrRef));
                    break;
            }
        }

        return format;
    }

    public override FormatBaseAttribute VisitBaseAttrRef([NotNull] Interlis24Parser.BaseAttrRefContext context)
    {
        // The attribute references are member references: they name members of the BASED ON structure, resolved
        // by the path resolver's member lookup rather than the scoped one.
        if (context.formatted != null)
        {
            // structureAttribute '/' formatted=definitionRef
            return new FormatBaseAttribute
            {
                Attribute = CreateMemberReference<AttributeDef>(context.structureAttribute),
                FormattedDomain = CreateReference<DomainDef>(context.formatted),
            };
        }

        // numericAttribute ('/' intPos=POS_NUMBER)?
        return new FormatBaseAttribute
        {
            Attribute = CreateMemberReference<AttributeDef>(context.numericAttribute),
            Position = context.intPos == null ? (int?)null : int.Parse(context.intPos.Text),
        };
    }

    public override TypeDef VisitDateTimeType([NotNull] Interlis24Parser.DateTimeTypeContext context)
    {
        // DATE / TIMEOFDAY / DATETIME are deliberately represented as references to the predefined domains they
        // denote (RefHB 3.8.7-4: they correspond to INTERLIS.XMLDate/XMLTime/XMLDateTime) — the uniform policy
        // for every type keyword that resolves to a real predefined definition (NAME, URI, BOOLEAN, the
        // alignments, the STRAIGHTS/ARCS line forms). The keyword form itself is not preserved: it carries no
        // semantics beyond the referenced domain.
        var domainName = context.kind.Type switch
        {
            Interlis24Parser.DATE => "XMLDate",
            Interlis24Parser.TIMEOFDAY => "XMLTime",
            Interlis24Parser.DATETIME => "XMLDateTime",
            _ => throw new UnexpectedNodeException(context.kind),
        };

        return new TypeRef { Extends = CreateReference<DomainDef>(ImpliedPath("INTERLIS", domainName)) };
    }

    public override Tuple<double, int> VisitExpNumber([NotNull] Interlis24Parser.ExpNumberContext context)
    {
        var number = context.EXP_NUMBER().Symbol.Text;
        var decPointIndex = number.IndexOf('.');
        var expIndex = number.IndexOfAny(['e', 'E']);

        // The precision of a mantissa (float) literal is its Stellenzahl — the number of mantissa digits — the same
        // measure decimal notation uses (RefHB 3.8.5-3/-4 compare the Stellenzahl); the magnitude (scaling) is
        // carried by the value itself.
        var decimalPrecision = expIndex - decPointIndex - 1;
        var value = double.Parse(number);

        return Tuple.Create(value, -decimalPrecision);
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
            SourceRange = context.ToRange(),
        };
    }

    public override CoordType VisitCoordinateType([NotNull] Interlis24Parser.CoordinateTypeContext context)
    {
        return new CoordType
        {
            IsMultiGeometry = context.MULTICOORD() != null,
            Axis = { context._axis.Select(VisitNumericType) },
            Rotation = context.rotationDef() == null ? null : VisitRotationDef(context.rotationDef()),
            RefSysCode = context.refsys == null ? null : VisitString(context.refsys),
            SourceRange = context.ToRange(),
        };
    }

    public override RotationDef VisitRotationDef([NotNull] Interlis24Parser.RotationDefContext context)
    {
        // Both axes are mandatory POS_NUMBERs but can be missing/synthetic while typing, which would fail int.Parse.
        return new RotationDef
        {
            NullAxis = IsValidToken(context.nullAxis) ? int.Parse(context.nullAxis.Text) : 0,
            PiHalfAxis = IsValidToken(context.piHalfAxis) ? int.Parse(context.piHalfAxis.Text) : 0,
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
            NameLocations = { (context.unitShortName == null ? context.unitTerm : context.unitShortName).ToRange() },
            SourceRange = context.ToRange(),
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
            Explanation = GetExplanation(context.derivedUnit()?.EXPLANATION()),
        };

        return unit;
    }

    public override IExpression VisitDerivedUnit([NotNull] Interlis24Parser.DerivedUnitContext context)
    {
        var derivedFrom = new UnitReferenceExpression { Unit = CreateReference<UnitDef>(context.definitionRef()), SourceRange = context.definitionRef()?.ToRange() };

        if (context.FUNCTION() != null)
        {
            // FUNCTION-defined unit: the conversion from the base unit is described by the EXPLANATION
            // (captured on the UnitDef), not a numeric formula, so the derivation is just the base unit.
            return derivedFrom;
        }

        var factors = context.decConst();
        if (factors.Length == 0)
        {
            return derivedFrom;
        }

        // Build the conversion factor as the written left-associative expression tree (e.g. 360 / 2 / PI).
        IExpression conversion = new NumericConstant { Value = VisitDecConst(factors[0]), SourceRange = factors[0].ToRange() };
        for (var i = 1; i < factors.Length; i++)
        {
            conversion = new ArithmeticExpression
            {
                SourceRange = factors[0].Start.ToRange(factors[i].Stop),
                Operator = context._op[i - 1].Type switch
                {
                    Interlis24Parser.ASTERISK => ArithmeticExpression.ArithmeticOperator.Multiplication,
                    Interlis24Parser.SLASH => ArithmeticExpression.ArithmeticOperator.Division,
                    _ => throw new UnexpectedNodeException(context._op[i - 1]),
                },
                FirstOperand = conversion,
                SecondOperand = new NumericConstant { Value = VisitDecConst(factors[i]), SourceRange = factors[i].ToRange() },
            };
        }

        // The derivation "factor * base unit" is implied by the syntax, so it spans the whole derived unit.
        return new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Multiplication, FirstOperand = conversion, SecondOperand = derivedFrom, SourceRange = context.ToRange() };
    }

    public override IExpression VisitComposedUnit([NotNull] Interlis24Parser.ComposedUnitContext context)
    {
        var unitRefs = context.definitionRef();
        var composedFrom = unitRefs.Select(d => new UnitReferenceExpression { Unit = CreateReference<UnitDef>(d), SourceRange = d.ToRange() }).ToArray();
        IExpression result = composedFrom[0];
        for (var i = 1; i < composedFrom.Length; i++)
        {
            var range = unitRefs[0].Start.ToRange(unitRefs[i].Stop);
            result = context._op[i - 1].Type switch
            {
                Interlis24Parser.ASTERISK => new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Multiplication, FirstOperand = result, SecondOperand = composedFrom[i], SourceRange = range },
                Interlis24Parser.SLASH => new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Division, FirstOperand = result, SecondOperand = composedFrom[i], SourceRange = range },
                _ => throw new UnexpectedNodeException(context._op[i - 1]),
            };
        }

        return result;
    }

    public override MetaDataBasketDef? VisitMetaDataBasketDef([NotNull] Interlis24Parser.MetaDataBasketDefContext context)
    {
        // The basket name is mandatory but may still be missing while typing (e.g. just 'SIGN BASKET'); without it
        // there is nothing to build. The enclosing SetContentDictionary drops the null (mirrors VisitModelDef).
        if (!IsValidToken(context.basketName))
        {
            return null;
        }

        var properties = VisitProperties(context.properties(), [Interlis24Parser.FINAL]);

        return new MetaDataBasketDef
        {
            Name = context.basketName.Text,
            NameLocations = { context.basketName.ToRange() },
            SourceRange = context.ToRange(),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            Properties = { properties },
            Kind = context.SIGN() != null ? MetaDataBasketDef.BasketKind.Sign : MetaDataBasketDef.BasketKind.Refsystem,
            Extends = CreateReference<MetaDataBasketDef>(context.extends),
            Topic = CreateReference<TopicDef>(context.topic),
            Objects = { context.metaObjectsDef().Select(VisitMetaObjectsDef) },
        };
    }

    public override MetaObjectsClause VisitMetaObjectsDef([NotNull] Interlis24Parser.MetaObjectsDefContext context)
    {
        return new MetaObjectsClause
        {
            // The class name is mandatory but may be missing right after 'OBJECTS OF' while typing. It is a member
            // reference: the class lives in the basket's topic, linked by the reference resolver.
            Class = IsValidToken(context.className)
                ? CreateMemberReference<ClassDef>(context.className)
                : new Reference<ClassDef>(),
            MetaObjects = { context._metaObjectName.Select(t => new MetaObjectDeclaration { Name = t.Text, NameLocations = { t.ToRange() } }) },
        };
    }

    public override List<ContextDef> VisitContextDef([NotNull] Interlis24Parser.ContextDefContext context)
    {
        return context.contextEntry().Select(VisitContextEntry).ToList();
    }

    public override ContextDef VisitContextEntry([NotNull] Interlis24Parser.ContextEntryContext context)
    {
        return new ContextDef
        {
            Name = context.name.Text,
            NameLocations = { context.name.ToRange() },
            SourceRange = context.ToRange(),
            Mappings = { context.contextMapping().Select(VisitContextMapping) },
        };
    }

    public override ContextMapping VisitContextMapping([NotNull] Interlis24Parser.ContextMappingContext context)
    {
        return new ContextMapping
        {
            GenericCoord = CreateReference<DomainDef>(context.genericCoordDef),
            Concrete = { context._concrete.Select(r => CreateReference<DomainDef>(r)).WhereNotNull() },
        };
    }

    public override List<DomainDef> VisitDomainDef([NotNull] Interlis24Parser.DomainDefContext context)
    {
        return context.domainTypeDef().Select(VisitDomainTypeDef).ToList();
    }

    public override DomainDef VisitDomainTypeDef([NotNull] Interlis24Parser.DomainTypeDefContext context)
    {
        var typeContext = context.type();
        var type = (typeContext != null ? VisitType(typeContext) as TypeDef : null) ?? new TypeRef();
        type.Cardinality = new Cardinality { Min = context.MANDATORY() == null ? 0 : 1, Max = Cardinality.Unbound };
        foreach (var constraintContext in context.domainConstraint())
        {
            // RefHB 3.8-8: every restriction has a unique name within its domain definition.
            var constraint = VisitDomainConstraint(constraintContext);
            if (!type.Constraints.TryAdd(constraint.Name, constraint))
            {
                ReportError(constraintContext.IDENTIFIER().Symbol, $"Duplicate domain constraint {constraint.Name}");
            }
        }

        var properties = VisitProperties(context.properties(), [Interlis24Parser.ABSTRACT, Interlis24Parser.GENERIC, Interlis24Parser.FINAL]);

        type.Extends = CreateReference<DomainDef>(context.extends);

        return new DomainDef
        {
            Name = context.name.Text,
            NameLocations = { context.name.ToRange() },
            SourceRange = context.ToRange(),
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
            Condition = Visit(context.expression()) as IExpression ?? new UndefinedConstant(),
            SourceRange = context.ToRange(),
        };
    }

    public override ConstraintDef? VisitConstraintDef([NotNull] Interlis24Parser.ConstraintDefContext context)
    {
        return VisitChildrenBase(context) as ConstraintDef;
    }

    /// <summary>
    /// Builds the constraints of the viewable currently in scope (the container pushed onto
    /// <c>CurrentScope</c> by the enclosing visit), setting each constraint's
    /// <see cref="IInterlisDefinition.Parent"/> so it is a fully-formed definition, and its
    /// <see cref="ConstraintDef.NameIndex"/> to its 1-based source position (mirroring ili2c's per-container
    /// constraint counter, which is used to synthesize names for anonymous constraints). Duplicate constraint
    /// names are legal (a constraint name is for messages only, not part of the namespace) but are poor model
    /// design, so each repeated name is reported as a warning (RefHB 3.12).
    /// </summary>
    private List<ConstraintDef> BuildConstraints(IEnumerable<Interlis24Parser.ConstraintDefContext> contexts)
    {
        var constraints = new List<ConstraintDef>();
        var index = 0;
        foreach (var context in contexts)
        {
            var constraint = VisitConstraintDef(context);
            if (constraint == null) continue;

            constraint.Parent = CurrentScope.Value;
            constraint.NameIndex = ++index;
            constraints.Add(constraint);
        }

        return constraints;
    }

    public override MandatoryConstraint? VisitMandatoryConstraint([NotNull] Interlis24Parser.MandatoryConstraintContext context)
    {
        // The condition is mandatory but may be missing (or visit to null) while typing. A constraint without a
        // condition carries no meaning and would break the type checker, so drop the whole constraint (BuildConstraints
        // tolerates the null).
        var condition = context.logical == null ? null : Visit(context.logical) as IExpression;
        if (condition == null)
        {
            return null;
        }

        return new MandatoryConstraint
        {
            Name = context.name?.Text ?? "",
            Condition = condition,
            SourceRange = context.ToRange(),
        };
    }

    public override PlausibilityConstraint? VisitPlausibilityConstraint([NotNull] Interlis24Parser.PlausibilityConstraintContext context)
    {
        // Percentage and condition are mandatory but either can be missing while typing. Without a condition the
        // constraint is meaningless (and would break the type checker), so drop the whole constraint.
        var condition = context.logical == null ? null : Visit(context.logical) as IExpression;
        if (condition == null)
        {
            return null;
        }

        var percentage = context.percentage != null && Visit(context.percentage) is Tuple<double, int> percentageValue ? percentageValue.Item1 : 0.0;
        return new PlausibilityConstraint
        {
            Name = context.name?.Text ?? "",
            Direction = context.GREATER_EQUAL() != null ? PlausibilityConstraint.Comparison.AtLeast : PlausibilityConstraint.Comparison.AtMost,
            Percentage = percentage,
            Condition = condition,
            SourceRange = context.ToRange(),
        };
    }

    public override ExistenceConstraint VisitExistenceConstraint([NotNull] Interlis24Parser.ExistenceConstraintContext context)
    {
        var paths = context.objectOrAttributePath();
        var refs = context.definitionRef();
        var constraint = new ExistenceConstraint
        {
            Name = context.name?.Text ?? "",
            // The checked path is mandatory but can be missing right after 'EXISTENCE CONSTRAINT' while typing.
            AttributePath = paths.Length > 0 ? VisitObjectOrAttributePath(paths[0]) : new PathExpression { Reference = new Reference<IInterlisDefinition> { Resolution = ReferenceResolution.ObjectPath } },
            SourceRange = context.ToRange(),
        };

        // REQUIRED IN ref ':' path { OR ref ':' path } — refs[i] pairs with paths[i + 1]. While typing there can be
        // more refs than paths (a trailing ref whose ':' path is not yet written), so stop when the path is missing.
        for (var i = 0; i < refs.Length && i + 1 < paths.Length; i++)
        {
            constraint.RequiredIn.Add(new ExistenceRequirement
            {
                Viewable = CreateReference<IInterlisDefinition>(refs[i]),
                AttributePath = VisitObjectOrAttributePath(paths[i + 1]),
            });
        }

        return constraint;
    }

    public override UniquenessConstraint VisitUniquenessConstraint([NotNull] Interlis24Parser.UniquenessConstraintContext context)
    {
        var constraint = new UniquenessConstraint
        {
            Name = context.name?.Text ?? "",
            IsBasket = context.BASKET() != null,
            Where = context.expression() == null ? null : Visit(context.expression()) as IExpression,
            SourceRange = context.ToRange(),
        };

        if (context.localUniqueness() != null)
        {
            constraint.Local = VisitLocalUniqueness(context.localUniqueness());
        }
        else if (context.uniqueEl() != null)
        {
            // Neither branch is present when only 'UNIQUE' has been typed so far.
            constraint.GlobalUnique.AddRange(context.uniqueEl().objectOrAttributePath().Select(VisitObjectOrAttributePath));
        }

        return constraint;
    }

    public override LocalUniqueness VisitLocalUniqueness([NotNull] Interlis24Parser.LocalUniquenessContext context)
    {
        // The structure path is an object path walked by the path resolver; the attribute names are members of the
        // substructure it reaches.
        var local = new LocalUniqueness
        {
            StructurePath = CreateReference<AttributeDef>(context._structureAttribute.Where(IsValidToken).Select(Segment), resolution: ReferenceResolution.ObjectPath),
        };
        local.AttributeNames.AddRange(context._attributeName.Where(IsValidToken)
            .Select(CreateMemberReference<AttributeDef>));
        return local;
    }

    public override SetConstraint? VisitSetConstraint([NotNull] Interlis24Parser.SetConstraintContext context)
    {
        var expressions = context.expression();
        var hasWhere = context.WHERE() != null;

        // '(WHERE expr ':')? expr' — the WHERE pre-condition (if any) is expressions[0], the mandatory condition is the
        // last expression. Either can still be unwritten while typing, so index defensively.
        var whereContext = hasWhere && expressions.Length > 0 ? expressions[0] : null;
        var conditionContext = hasWhere
            ? (expressions.Length > 1 ? expressions[1] : null)
            : (expressions.Length > 0 ? expressions[0] : null);

        var condition = conditionContext == null ? null : Visit(conditionContext) as IExpression;
        if (condition == null)
        {
            // A set constraint without a condition is meaningless and would break the type checker; drop it.
            return null;
        }

        return new SetConstraint
        {
            Name = context.name?.Text ?? "",
            IsBasket = context.BASKET() != null,
            Where = whereContext == null ? null : Visit(whereContext) as IExpression,
            Condition = condition,
            SourceRange = context.ToRange(),
        };
    }

    /// <summary>
    /// Builds the <c>CONSTRAINTS OF</c> blocks of a topic (RefHB 3.12-41). Each block is unnamed in source, so a
    /// unique name <c>CONSTRAINTS OF &lt;target&gt; #&lt;n&gt;</c> is synthesized — disambiguated per target, so several
    /// blocks for the same class get <c>#1</c>, <c>#2</c>, … The spaces and <c>#</c> make the name impossible as a
    /// user identifier, so it can never collide with a real definition in the topic's content.
    /// The block is an <see cref="IInterlisDefinitionContainer"/>, so its constraints (and the references inside them)
    /// are parented/scoped to the block rather than to the enclosing topic. The <c>CONSTRAINTS OF</c> target itself is
    /// resolved in the topic scope, so it is created before the block scope is entered.
    /// </summary>
    private List<ConstraintsBlockDef> BuildConstraintsBlocks(IEnumerable<Interlis24Parser.ConstraintsDefContext> contexts)
    {
        var blocks = new List<ConstraintsBlockDef>();
        var ordinals = new Dictionary<string, int>();
        foreach (var context in contexts)
        {
            var target = CreateReference<IInterlisDefinition>(context.definitionRef());
            var targetText = target == null ? "?" : string.Join(".", target.Path);
            var ordinal = ordinals[targetText] = ordinals.GetValueOrDefault(targetText) + 1;
            var block = new ConstraintsBlockDef
            {
                Name = $"CONSTRAINTS OF {targetText} #{ordinal}",
                SourceRange = context.ToRange(),
                Target = target,
                Parent = CurrentScope.Value,
            };

            using (CurrentScope.NewFrame(block))
            {
                block.Constraints.AddRange(BuildConstraints(context.constraintDef()));
            }

            blocks.Add(block);
        }

        return blocks;
    }

    public override TypeDef VisitOidType([NotNull] Interlis24Parser.OidTypeContext context)
    {
        // The inner type can be missing after a syntax error (a bare 'OID'); the value stays null then.
        return new OidType
        {
            Value = context.ANY() != null
                ? new OidType.AnyOid()
                : VisitChildrenBase(context) is TypeDef inner ? new OidType.ValueRange { Type = inner } : null,
            SourceRange = context.ToRange(),
        };
    }

    public override ClassType VisitClassType([NotNull] Interlis24Parser.ClassTypeContext context)
    {
        return new ClassType
        {
            IsStructure = context.STRUCTURE() != null,
            Restrictions = { context.definitionRef().Select(r => CreateReference<IInterlisDefinition>(r)).WhereNotNull() },
            SourceRange = context.ToRange(),
        };
    }

    public override AttributePathType VisitAttributePathType([NotNull] Interlis24Parser.AttributePathTypeContext context)
    {
        return new AttributePathType
        {
            Of = context.objectOrAttributePath() == null ? null : VisitObjectOrAttributePath(context.objectOrAttributePath()),
            ArgumentName = context.argumentName?.Text,
            Restrictions = { context.attrTypeDef().Select(VisitAttrTypeDef) },
            SourceRange = context.ToRange(),
        };
    }

    public override TypeDef VisitBlackboxType([NotNull] Interlis24Parser.BlackboxTypeContext context)
    {
        return new BlackboxType
        {
            Kind = context.XML() != null ? BlackboxType.BlackboxTypeKind.Xml : BlackboxType.BlackboxTypeKind.Binary,
            SourceRange = context.ToRange(),
        };
    }

    public override TypeDef VisitAlignmentType([NotNull] Interlis24Parser.AlignmentTypeContext context)
    {
        return new TypeRef { Extends = CreateReference<DomainDef>(ImpliedPath("INTERLIS", context.GetText())) };
    }

    public override TypeDef VisitLineType([NotNull] Interlis24Parser.LineTypeContext context)
    {
        var lineForms = context.lineForm() != null ? VisitLineForm(context.lineForm()) : [];

        // The presence of 'WITHOUT OVERLAPS' is meaningful on its own (RefHB 3.8.12.2-22); the '> numeric'
        // tolerance is optional, and the number can still be missing right after '>' while typing.
        WithoutOverlapsDef? withoutOverlaps = context.WITHOUT() == null ? null
            : context.numeric() != null && Visit(context.numeric()) is Tuple<double, int> overlapValue
                ? new WithoutOverlapsDef.Explicit { Tolerance = overlapValue.Item1 }
                : new WithoutOverlapsDef.Implicit();

        if (context.POLYLINE() != null || context.MULTIPOLYLINE() != null)
        {
            return new PolyLineType
            {
                IsMultiGeometry = context.MULTIPOLYLINE() != null,
                IsDirected = context.DIRECTED() != null,
                WithoutOverlaps = withoutOverlaps,
                LineForms = { lineForms },
                VertexType = CreateReference<DomainDef>(context.vertexType),
                SourceRange = context.ToRange(),
            };
        }
        else
        {
            return new SurfaceType
            {
                IsMultiGeometry = context.MULTIAREA() != null || context.MULTISURFACE() != null,
                IsCoverage = context.AREA() != null || context.MULTIAREA() != null,
                WithoutOverlaps = withoutOverlaps,
                LineForms = { lineForms },
                VertexType = CreateReference<DomainDef>(context.vertexType),
                SourceRange = context.ToRange(),
            };
        }
    }

    public override List<Reference<LineFormTypeDef>> VisitLineForm([NotNull] Interlis24Parser.LineFormContext context)
    {
        // The written order and duplicates are preserved (RefHB 3.8.12.2-29 puts no uniqueness rule on the list).
        return context.lineFormType().Select(VisitLineFormType).WhereNotNull().ToList();
    }

    public override Reference<LineFormTypeDef>? VisitLineFormType([NotNull] Interlis24Parser.LineFormTypeContext context)
    {
        // The STRAIGHTS / ARCS keywords denote the predefined INTERLIS line forms (RefHB 3.8.12.1) and become
        // references into the internal model — like the predefined domain keywords (see VisitAlignmentType) — so
        // every line form resolves uniformly to its LineFormTypeDef.
        if (context.STRAIGHTS() != null || context.ARCS() != null)
        {
            // The keyword is the predefined form's own name, so the written token is the segment.
            return CreateReference<LineFormTypeDef>([new PathSegment { Name = InternalModel.Interlis.Name }, Segment(context.Start)]);
        }

        // A custom form is a (possibly model-qualified) reference to a LINE FORM type (RefHB 3.8.12.3). Its name
        // can still be unwritten (or an error-recovery token) while typing; drop it — the parse error is already
        // reported.
        return context.definitionRef() is { } custom && IsValidToken(custom.name)
            ? CreateReference<LineFormTypeDef>(custom)
            : null;
    }

    public override List<LineFormTypeDef> VisitLineFormTypeDef([NotNull] Interlis24Parser.LineFormTypeDefContext context)
    {
        // LINE FORM (lineFormTypeName ':' lineStructureName ';')* — IDENTIFIERs come in (name, structure) pairs.
        var identifiers = context.IDENTIFIER();
        var result = new List<LineFormTypeDef>();
        for (var i = 0; i + 1 < identifiers.Length; i += 2)
        {
            result.Add(new LineFormTypeDef
            {
                Name = identifiers[i].GetText(),
                NameLocations = { identifiers[i].Symbol.ToRange() },
                SourceRange = identifiers[i].Symbol.ToRange(identifiers[i + 1].Symbol),
                Structure = CreateReference<ClassDef>([Segment(identifiers[i + 1].Symbol)]),
            });
        }

        return result;
    }

    public override EnumerationValuesType VisitEnumTreeValueType([NotNull] Interlis24Parser.EnumTreeValueTypeContext context)
    {
        // ALL OF admits every node and leaf of the referenced enumeration (RefHB 3.8.3).
        return new EnumerationValuesType
        {
            TargetEnumeration = CreateReference<DomainDef>(context.definitionRef()),
            LeafsOnly = false,
            SourceRange = context.ToRange(),
        };
    }

    public override EnumerationType VisitEnumerationType([NotNull] Interlis24Parser.EnumerationTypeContext context)
    {
        return new EnumerationType
        {
            Sequencing = (EnumerationType.Sequencings)(context.sequencing?.Type ?? 0),
            Values = { VisitEnumeration(context.enumeration()) },
            SourceRange = context.ToRange(),
        };
    }

    public override EnumerationValuesList VisitEnumeration([NotNull] Interlis24Parser.EnumerationContext context)
    {
        var values = new EnumerationValuesList { context.enumElement().Select(VisitEnumElement).WhereNotNull() };
        values.IsFinal = context.FINAL() != null;
        return values;
    }

    public override EnumerationTreeNode? VisitEnumElement([NotNull] Interlis24Parser.EnumElementContext context)
    {
        var identifiers = context.IDENTIFIER();
        if (identifiers.Length == 0)
        {
            // An empty enum element occurs while typing (e.g. '(red,' with the next value not written yet); drop it.
            return null;
        }

        EnumerationTreeNode root, leaf = root = new EnumerationTreeNode { Name = identifiers.First().GetText() };
        foreach (var value in identifiers.Skip(1))
        {
            var node = new EnumerationTreeNode { Name = value.GetText(), FromDottedName = true };
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
        // 'META_ATTR_NAME '=' (META_ATTR_NAME | string)' — while typing the value (child 2, or even the whole
        // attribute) may not be there yet.
        var key = context.GetChild(0)?.GetText() ?? string.Empty;
        var valueContext = context.GetChild(2);
        var value = valueContext == null ? string.Empty : Visit(valueContext) as string ?? valueContext.GetText();

        return Tuple.Create(key, value);
    }

    public override string VisitString([NotNull] Interlis24Parser.StringContext context)
    {
        var sb = new StringBuilder();
        foreach (var child in context.children ?? [])
        {
            if (child is IErrorNode)
            {
                // Skip error nodes
                continue;
            }
            else if (child is TerminalNodeImpl terminal)
            {
                // Terminal symbols don't have individual visit methods.
                switch (terminal.Symbol.Type)
                {
                    case Interlis24Parser.DOUBLE_QUOTE_OPEN:
                    case Interlis24Parser.DOUBLE_QUOTE_CLOSE:
                        // Ignore opening and closing quotes
                        break;

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

                    case Interlis24Parser.LITERAL_NEWLINE:
                        ReportError(terminal.Symbol, $"Strings cannot span multiple lines");
                        sb.Append(terminal.GetText());
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
            // The bound token can be absent while typing (e.g. just '{' or 'CARDINALITY =' with no braces yet).
            if (token == null)
            {
                return Cardinality.Unbound;
            }

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
        CheckOperatorNotChained(context);

        // An operand can be missing or unparseable while typing; UndefinedConstant keeps the node total.
        var first = Visit(context.expression(0)) as IExpression ?? new UndefinedConstant();
        var second = Visit(context.expression(1)) as IExpression ?? new UndefinedConstant();
        var range = context.ToRange();

        return (context.binOp.Type) switch
        {
            // Comparison operators keep the written operator and operand order (no desugaring): `<` is not a
            // swapped `>`, and `<>` is not a negated `==`.
            Interlis24Parser.DOUBLE_EQUAL => new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.NOT_EQUAL => new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.NotEqual, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.GREATER => new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.LESSER => new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Less, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.GREATER_EQUAL => new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.GreaterOrEqual, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.LESS_EQUAL => new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.LessOrEqual, FirstOperand = first, SecondOperand = second, SourceRange = range },

            Interlis24Parser.AND => new LogicalExpression { Operator = LogicalExpression.LogicalOperator.And, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.OR => new LogicalExpression { Operator = LogicalExpression.LogicalOperator.Or, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.FAT_ARROW => new LogicalExpression { Operator = LogicalExpression.LogicalOperator.Implication, FirstOperand = first, SecondOperand = second, SourceRange = range },

            Interlis24Parser.PLUS => new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Addition, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.HYPHEN => new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Subtraction, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.ASTERISK => new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Multiplication, FirstOperand = first, SecondOperand = second, SourceRange = range },
            Interlis24Parser.SLASH => new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Division, FirstOperand = first, SecondOperand = second, SourceRange = range },

            _ => throw new NotImplementedException($"Operator {Interlis24Parser.DefaultVocabulary.GetDisplayName(context.binOp.Type)} not implemented"),
        };
    }

    /// <summary>
    /// RefHB 3.13-3/-6: a relation (<c>Term2 = Predicate [ Relation Predicate ]</c>) and an implication
    /// (<c>Term = Term0 [ '=&gt;' Term0 ]</c>) are non-associative — at most one may appear without parentheses. The
    /// grammar accepts chains for simplicity (a plain left-recursive rule); this restores the handbook constraint.
    /// A parenthesised sub-expression parses as a <c>notExpression</c>, not a <c>binaryExpression</c>, so grouped
    /// forms such as <c>a &lt; (b &lt; c)</c> and <c>a =&gt; (b =&gt; c)</c> remain valid.
    /// </summary>
    private void CheckOperatorNotChained(Interlis24Parser.BinaryExpressionContext context)
    {
        var operatorType = context.binOp.Type;
        if (IsComparisonOperator(operatorType) && HasDirectlyNestedOperand(context, IsComparisonOperator))
        {
            ReportError(context.binOp, "comparison operators can not be chained (use parentheses)");
        }
        else if (operatorType == Interlis24Parser.FAT_ARROW && HasDirectlyNestedOperand(context, type => type == Interlis24Parser.FAT_ARROW))
        {
            ReportError(context.binOp, "the implication operator '=>' can not be chained (use parentheses)");
        }
    }

    private static bool IsComparisonOperator(int tokenType) => tokenType
        is Interlis24Parser.DOUBLE_EQUAL
        or Interlis24Parser.NOT_EQUAL
        or Interlis24Parser.LESSER
        or Interlis24Parser.LESS_EQUAL
        or Interlis24Parser.GREATER
        or Interlis24Parser.GREATER_EQUAL;

    /// <summary>
    /// Whether a direct operand of <paramref name="context"/> is itself a binary expression whose operator matches
    /// <paramref name="isSameCategory"/> — i.e. an un-parenthesised nested relation/implication. A parenthesised
    /// operand is a <c>notExpression</c> (not a <c>binaryExpression</c>) and is therefore ignored.
    /// </summary>
    private static bool HasDirectlyNestedOperand(Interlis24Parser.BinaryExpressionContext context, Func<int, bool> isSameCategory)
    {
        return new[] { context.expression(0), context.expression(1) }
            .OfType<Interlis24Parser.BinaryExpressionContext>()
            .Any(operand => isSameCategory(operand.binOp.Type));
    }

    public override IExpression VisitFactorExpression([NotNull] Interlis24Parser.FactorExpressionContext context)
    {
        return VisitFactor(context.factor());
    }

    public override IExpression? VisitNotExpression([NotNull] Interlis24Parser.NotExpressionContext context)
    {
        // The parenthesised expression can still be unwritten (e.g. 'NOT' or '(' at the caret); without an operand
        // there is no expression to build.
        var expressionContext = context.expression();
        var expression = expressionContext == null ? null : Visit(expressionContext) as IExpression;
        if (expression == null)
        {
            return null;
        }

        return context.NOT() == null ? expression : new NotExpression { Operand = expression, SourceRange = context.ToRange() };
    }

    public override IExpression? VisitDefinedExpression([NotNull] Interlis24Parser.DefinedExpressionContext context)
    {
        // 'DEFINED '(' factor ')'' — the factor can still be unwritten while typing.
        var operand = context.factor() == null ? null : VisitFactor(context.factor());
        return operand == null ? null : new DefinedExpression { Operand = operand, SourceRange = context.ToRange() };
    }

    public override IExpression VisitFactor([NotNull] Interlis24Parser.FactorContext context)
    {
        // The inspection alternative ('(inspection | INSPECTION definitionRef) (OF objectOrAttributePath)?',
        // RefHB 3.13-25) builds no expression through the child walk; a direct INSPECTION token only occurs in its
        // named form (the inline form's token sits inside the inspection subrule).
        if (context.inspection() != null || context.INSPECTION() != null)
        {
            return BuildInspectionExpression(context);
        }

        // The 'PARAMETER definitionRef' alternative (RefHB 3.13-25) references a run-time parameter; the name can
        // still be unwritten (or an error-recovery token) while typing, in which case there is nothing to reference.
        if (context.PARAMETER() != null)
        {
            return context.definitionRef() is { } parameter && IsValidToken(parameter.name)
                ? new ParameterRefExpression { Parameter = CreateReference<ParameterDef>(parameter), SourceRange = context.ToRange() }
                : new UndefinedConstant();
        }

        // Most factor alternatives visit to an IExpression. A factor that is still incomplete while typing
        // aggregates to a non-expression or null; fall back to an undefined constant so callers get a usable value.
        return VisitChildrenBase(context) as IExpression ?? new UndefinedConstant();
    }

    /// <summary>
    /// Builds the <see cref="InspectionExpression"/> for a factor's inspection alternative (RefHB 3.13-25/-48):
    /// an inline inspection or a reference to a named inspection view, with the optional <c>OF</c> restriction path.
    /// </summary>
    private IExpression BuildInspectionExpression(Interlis24Parser.FactorContext context)
    {
        InspectionExpression.InspectionSource? source = null;
        if (context.inspection() is { } inline)
        {
            source = BuildInspectionView(inline);
        }
        // The referenced viewable must be an inspection view, but resolution accepts any viewable so the
        // path resolver can report a non-inspection target by name instead of leaving it unresolved.
        else if (context.definitionRef() is { } viewRef && IsValidToken(viewRef.name))
        {
            source = CreateReference<IInterlisDefinition>(viewRef, AcceptViewable);
        }

        // 'INSPECTION' whose view reference is still unwritten (or an error-recovery token) while typing; fall back
        // like other incomplete factors.
        if (source == null)
        {
            return new UndefinedConstant();
        }

        return new InspectionExpression
        {
            Source = source,
            Of = context.objectOrAttributePath() is { } ofPath ? VisitObjectOrAttributePath(ofPath) : null,
            SourceRange = context.ToRange(),
        };
    }

    public override ConstantExpression VisitConstant([NotNull] Interlis24Parser.ConstantContext context)
    {
        if (context.@string() != null) return new TextConstant { Value = VisitString(context.@string()), SourceRange = context.ToRange() };
        if (context.UNDEFINED() != null) return new UndefinedConstant { SourceRange = context.ToRange() };

        return VisitChildrenBase(context) as ConstantExpression ?? new UndefinedConstant();
    }

    public override NumericConstant VisitNumericConst([NotNull] Interlis24Parser.NumericConstContext context)
    {
        return new NumericConstant
        {
            Value = VisitDecConst(context.decConst()),
            Unit = CreateReference<UnitDef>(context.definitionRef()),
            SourceRange = context.ToRange(),
        };
    }

    public override EnumerationConstant VisitEnumerationConst([NotNull] Interlis24Parser.EnumerationConstContext context)
    {
        return new EnumerationConstant
        {
            Path = { context.IDENTIFIER().Select(e => e.GetText()) },
            IsOthers = context.OTHERS() != null,
            SourceRange = context.ToRange(),
        };
    }

    /// <summary>
    /// The value a <c>decConst</c> denotes: a symbolic <see cref="NumericConstant.MathematicalConstant"/> for the
    /// predefined <c>PI</c>/<c>LNBASE</c>, otherwise a <see cref="NumericConstant.Number"/> with the written numeric value.
    /// </summary>
    public override NumericConstant.NumericValue VisitDecConst([NotNull] Interlis24Parser.DecConstContext context)
    {
        if (context.PI() != null) return new NumericConstant.MathematicalConstant { Kind = NumericConstant.PredefinedConstant.Pi };
        if (context.LNBASE() != null) return new NumericConstant.MathematicalConstant { Kind = NumericConstant.PredefinedConstant.LnBase };

        // The number can still be unwritten while typing (e.g. after a '*' in a derived unit or an arithmetic factor).
        var value = context.numeric() != null && Visit(context.numeric()) is Tuple<double, int> number ? number.Item1 : 0.0;
        return new NumericConstant.Number { Value = value };
    }

    public override AttributePathConstant VisitAttributePathConst([NotNull] Interlis24Parser.AttributePathConstContext context)
    {
        // '>>' [ ViewableRef '->' ] Attribute-Name is a single reference to the attribute: the optional viewable path
        // plus the attribute name. When the viewable is omitted the attribute belongs to the current object's class.
        // The attribute name can still be unwritten while typing (e.g. just '>>'); build the reference from whatever
        // is present.
        IEnumerable<PathSegment> definitionPath = context.definitionRef() == null ? [] : VisitDefinitionRef(context.definitionRef());
        IEnumerable<PathSegment> path = IsValidToken(context.attribute) ? definitionPath.Append(Segment(context.attribute)) : definitionPath;

        return new AttributePathConstant { Attribute = CreateReference<AttributeDef>(path), SourceRange = context.ToRange() };
    }

    public override ClassConstant VisitClassConst([NotNull] Interlis24Parser.ClassConstContext context)
    {
        return new ClassConstant { Viewable = CreateReference<IInterlisDefinition>(context.definitionRef()), SourceRange = context.ToRange() };
    }

    public override PathExpression VisitObjectOrAttributePath([NotNull] Interlis24Parser.ObjectOrAttributePathContext context)
    {
        return new PathExpression
        {
            Reference = CreateReference<IInterlisDefinition>(context.pathEl().Select(VisitPathEl).WhereNotNull(), resolution: ReferenceResolution.ObjectPath),
            SourceRange = context.ToRange(),
        };
    }

    /// <summary>
    /// Builds the most specific segment a single <c>PathEl</c> allows (RefHB 3.13). Each <c>PathEl</c> maps to
    /// exactly one segment: a keyword step (<see cref="KeywordPathSegment"/>), an association-qualified role
    /// (<see cref="RolePathSegment"/>), an indexed attribute (<see cref="IndexedPathSegment"/>) or a bare name (a
    /// plain <see cref="PathSegment"/>, whose kind — attribute/role/base/reference-attribute — is resolved later).
    /// <see langword="null"/> for a step with nothing written yet (a trailing <c>-&gt;</c> while typing): there is
    /// no name to register, and the parse error is already reported. The optional <c>'\'</c> on the
    /// association-access form carries no meaning documented in the RefHB and is ignored.
    /// </summary>
    public override PathSegment? VisitPathEl([NotNull] Interlis24Parser.PathElContext context)
    {
        if (context.name == null)
        {
            return context.keyword == null ? null : new KeywordPathSegment((PathKeyword)context.keyword.Type) { Range = context.keyword.ToRange() };
        }

        if (context.detail == null)
        {
            return Segment(context.name);
        }

        // '[' IDENTIFIER ']' qualifies a role by its association; '[' FIRST | LAST | PosNumber ']' indexes an attribute.
        return context.detail.Type switch
        {
            Interlis24Parser.IDENTIFIER => new RolePathSegment { Name = context.name.Text, Range = context.name.ToRange(), Association = Segment(context.detail) },
            Interlis24Parser.FIRST => new IndexedPathSegment { Name = context.name.Text, Range = context.name.ToRange(), Index = IndexKeyword.First },
            Interlis24Parser.LAST => new IndexedPathSegment { Name = context.name.Text, Range = context.name.ToRange(), Index = IndexKeyword.Last },
            // The numeric index can be a missing/synthetic token while typing (e.g. just '['), which would fail int.Parse.
            _ => new IndexedPathSegment { Name = context.name.Text, Range = context.name.ToRange(), Index = int.TryParse(context.detail.Text, out var index) ? index : 0 },
        };
    }

    public override object? VisitFunctionDef([NotNull] Interlis24Parser.FunctionDefContext context)
    {
        // The function name is mandatory but may still be missing while typing; without it there is nothing to build
        // (mirrors VisitModelDef). The enclosing SetContentDictionary drops the null.
        if (!IsValidToken(context.name))
        {
            return null;
        }

        return new FunctionDef
        {
            Name = context.name.Text,
            NameLocations = { context.name.ToRange() },
            SourceRange = context.ToRange(),
            DocComments = { GetDocComments(context) },
            MetaAttributes = { ProcessMetaAttributes(context) },
            // The return type can be missing right after the ':' while typing; fall back to the undefined type.
            ReturnType = context.returnType == null ? UndefinedType.Instance : VisitArgumentType(context.returnType),
            Arguments = { context.functionArgument().Select(VisitFunctionArgument) },
            Explanation = GetExplanation(context.EXPLANATION()),
        };
    }

    public override FunctionArgument VisitFunctionArgument([NotNull] Interlis24Parser.FunctionArgumentContext context)
    {
        // Both the argument name and its type can still be unwritten while typing (e.g. 'f(a' or a trailing ';').
        return new FunctionArgument
        {
            Name = IsValidToken(context.argumentName) ? context.argumentName.Text : string.Empty,
            Type = context.argumentType() == null ? UndefinedType.Instance : VisitArgumentType(context.argumentType()),
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
            // The target restriction (RefHB 3.14-11 ArgumentType) resolves against the function's enclosing
            // scope, like ili2c. An ANY placeholder is preserved as written — ANYCLASS and ANYSTRUCTURE must stay
            // distinguishable, only the former is legal here (RefHB 3.14-12, RestrictedClassOrAssRef; enforced by
            // the type checker). The ref context may be missing mid-typing.
            // OBJECT takes exactly one object, OBJECTS a possibly-empty set — carried by the cardinality, the
            // same way BAG/LIST express collections (a set argument is what ALL produces at a call site).
            var target = context.restrictedDefinitionRef() is { } targetContext ? VisitRestrictedDefinitionRef(targetContext) : null;
            return new ObjectType
            {
                Targets = target != null ? [target] : [],
                Cardinality = context.OBJECTS() != null
                    ? new Cardinality { Min = 0, Max = Cardinality.Unbound }
                    : new Cardinality { Min = 1, Max = 1 },
                SourceRange = context.ToRange(),
            };
        }
        else
        {
            // ENUMVAL accepts only leaf values, ENUMTREEVAL also the tree's node values (RefHB 3.14-7/-8) —
            // of any enumeration, so no TargetEnumeration.
            return new EnumerationValuesType
            {
                LeafsOnly = context.ENUMVAL() != null,
                SourceRange = context.ToRange(),
            };
        }
    }

    public override object VisitFunctionCall([NotNull] Interlis24Parser.FunctionCallContext context)
    {
        return new FunctionCall
        {
            FunctionDef = CreateReference<FunctionDef>(context.definitionRef()),
            Arguments = { context.argument().Select(VisitArgument) },
            SourceRange = context.ToRange(),
        };
    }

    public override IExpression VisitArgument([NotNull] Interlis24Parser.ArgumentContext context)
    {
        if (context.expression() != null)
        {
            return Visit(context.expression()) as IExpression ?? new UndefinedConstant();
        }
        else
        {
            return new AllExpression(context.restrictedDefinitionRef() != null ? VisitRestrictedDefinitionRef(context.restrictedDefinitionRef()) : null) { SourceRange = context.ToRange() };
        }
    }
}
