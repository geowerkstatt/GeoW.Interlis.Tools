using Antlr4.Runtime;
using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools.AST.Types;
using Geowerkstatt.Interlis.Tools.CreateAST;

namespace Geowerkstatt.Interlis.Tools;

public class InterlisReader
{
    public IErrorListenerProvider ErrorListenerProvider { get; set; } = new ThrowingErrorListenerProvider();

    public static readonly ModelDef InternalInterlisModel;

    static InterlisReader()
    {
        InternalInterlisModel = new ModelDef
        {
            Name = "INTERLIS",
            Content =
            {
                {
                    "m",
                    new UnitDef
                    {
                        Name = "m",
                        Term = "METER",
                    }
                },
                {
                    "URI",
                    new DomainDef
                    {
                        Name = "URI",
                        TypeDef = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            Length = 1023,
                        },
                    }
                },
                {
                    "NAME",
                    new DomainDef
                    {
                        Name = "NAME",
                        TypeDef = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            Length = 255,
                        },
                    }
                },
                {
                    "BOOLEAN",
                    new DomainDef
                    {
                        Name = "BOOLEAN",
                        TypeDef = new EnumerationType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            Sequencing = EnumerationType.Sequencings.Ordered,
                            Values =
                            {
                                new EnumerationTreeNode { Name = "false" },
                                new EnumerationTreeNode { Name = "true" },
                            },
                        },
                    }
                },
                {
                    "HALIGNMENT",
                    new DomainDef
                    {
                        Name = "HALIGNMENT",
                        TypeDef = new EnumerationType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            Sequencing = EnumerationType.Sequencings.Ordered,
                            Values =
                            {
                                new EnumerationTreeNode { Name = "Left" },
                                new EnumerationTreeNode { Name = "Center" },
                                new EnumerationTreeNode { Name = "Right" },
                            },
                        },
                    }
                },
                {
                    "VALIGNMENT",
                    new DomainDef
                    {
                        Name = "VALIGNMENT",
                        TypeDef = new EnumerationType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            Sequencing = EnumerationType.Sequencings.Ordered,
                            Values =
                            {
                                new EnumerationTreeNode { Name = "Top" },
                                new EnumerationTreeNode { Name = "Cap" },
                                new EnumerationTreeNode { Name = "Half" },
                                new EnumerationTreeNode { Name = "Base" },
                                new EnumerationTreeNode { Name = "Bottom" },
                            },
                        },
                    }
                },
            }
        };
    }

    /// <summary>
    /// Compiles the content of the <paramref name="textReader"/> to a <see cref="InterlisFile"/>.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <returns>The compiled representation of the <paramref name="textReader"/> input.</returns>
    public InterlisFile ReadFile(TextReader textReader)
    {
        var (interlisFile, unresolvedReferences) = ReadRule(textReader, (p, v) => v.VisitInterlis(p.interlis()));

        // resolve model imports
        var modelDefs = interlisFile.Content.Values.OfType<ModelDef>().ToList();
        var availableModels = modelDefs.ToDictionary(m => m.Name);
        availableModels["INTERLIS"] = InternalInterlisModel;

        foreach (var model in modelDefs)
        {
            foreach (var import in model.Imports)
            {
                if (availableModels.TryGetValue(import.Key, out var importedModel))
                {
                    model.Imports[import.Key] = (import.Value.IsUnqualifiedAllowed, importedModel);
                }
            }
        }

        // resolve references
        foreach (var reference in unresolvedReferences)
        {
            reference.TryResolve();
        }

        return interlisFile;
    }

    /// <summary>
    /// Compiles the content of the <paramref name="textReader"/> using the <paramref name="parseRule"/> function.
    /// </summary>
    /// <param name="textReader">The input to compile.</param>
    /// <param name="parseRule">A function to parse the input given the <see cref="Interlis24Parser"/> and <see cref="Interlis24Visitor"/>.</param>
    /// <returns>The compiled representation of the <paramref name="textReader"/> input and a list of <see cref="UnresolvedReference"/>s.</returns>
    public (TResult, List<UnresolvedReference>) ReadRule<TResult>(TextReader textReader, Func<Interlis24Parser, Interlis24Visitor, TResult> parseRule)
    {
        var inputStream = CharStreams.fromTextReader(textReader);

        var interlisLexer = new Interlis24Lexer(inputStream);
        interlisLexer.RemoveErrorListeners();
        interlisLexer.AddErrorListener(ErrorListenerProvider.GetErrorListener<int>());

        var interlisParser = new Interlis24Parser(new CommonTokenStream(interlisLexer));
        interlisParser.RemoveErrorListeners();
        interlisParser.AddErrorListener(ErrorListenerProvider.GetErrorListener<IToken>());

        var visitor = new Interlis24Visitor(ErrorListenerProvider.GetErrorListener<IToken>());
        return (parseRule(interlisParser, visitor), visitor.ReferencesToResolve);
    }
}
