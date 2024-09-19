using Geowerkstatt.Interlis.Tools.AST;
using Geowerkstatt.Interlis.Tools.AST.Types;
using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Tools.CreateAST;

/// <summary>
/// Resolves various references inside the AST. The AST is modified in place.
/// </summary>
public class Interlis24AstReferenceResolverVisitor : Interlis24AstBaseVisitor<object>
{
    private List<UnresolvedReference> ReferencesToResolve { get; }

    public static readonly ModelDef InternalInterlisModel;

    static Interlis24AstReferenceResolverVisitor()
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

    public Interlis24AstReferenceResolverVisitor(List<UnresolvedReference> unresolvedReferences)
    {
        ReferencesToResolve = unresolvedReferences;
    }

    public override InterlisFile VisitInterlisFile([NotNull] InterlisFile interlisFile)
    {
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
        foreach (var reference in ReferencesToResolve)
        {
            reference.TryResolve();
        }

        // visit children
        base.VisitInterlisFile(interlisFile);

        return interlisFile;
    }
}
