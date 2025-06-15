using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Resolves various references inside the AST. The AST is modified in place.
/// </summary>
public class Interlis24AstReferenceResolverVisitor(ILoggerFactory loggerFactory, List<IUnresolvedReference> referencesToResolve) : Interlis24AstBaseVisitor<object>
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Interlis24AstReferenceResolverVisitor>();

    /// <summary>
    /// The internal INTERLIS model that is always available.
    /// </summary>
    public static readonly ModelDef InternalInterlisModel;

    static Interlis24AstReferenceResolverVisitor()
    {
        var unitLength = new UnitDef { Name = "LENGTH", Term = "LENGTH", Properties = { Property.Abstract } };
        var unitMass = new UnitDef { Name = "MASS", Term = "MASS", Properties = { Property.Abstract } };
        var unitTime = new UnitDef { Name = "TIME", Term = "TIME", Properties = { Property.Abstract } };
        var unitElectricCurrent = new UnitDef { Name = "ELECTRIC_CURRENT", Term = "ELECTRIC_CURRENT", Properties = { Property.Abstract } };
        var unitTemperature = new UnitDef { Name = "TEMPERATURE", Term = "TEMPERATURE", Properties = { Property.Abstract } };
        var unitAmountOfMatter = new UnitDef { Name = "AMOUNT_OF_MATTER", Term = "AMOUNT_OF_MATTER", Properties = { Property.Abstract } };
        var unitAngle = new UnitDef { Name = "ANGLE", Term = "ANGLE", Properties = { Property.Abstract } };
        var unitSolidAngle = new UnitDef { Name = "SOLID_ANGLE", Term = "SOLID_ANGLE", Properties = { Property.Abstract } };
        var unitLuminousIntensity = new UnitDef { Name = "LUMINOUS_INTENSITY", Term = "LUMINOUS_INTENSITY", Properties = { Property.Abstract } };

        var timeOfDay = new ClassDef
        {
            Name = "TimeOfDay",
            IsStructure = true,
        };
        var utc = new ClassDef
        {
            Name = "UTC",
            IsStructure = true,
            Extends = new Reference<ClassDef> { Target = timeOfDay },
        };
        var gregorianDate = new ClassDef
        {
            Name = "GregorianDate",
            IsStructure = true,
        };
        var gregorianDateTime = new ClassDef
        {
            Name = "GregorianDateTime",
            IsStructure = true,
            Extends = new Reference<ClassDef> { Target = gregorianDate },
        };

        var xmlDate = new FormattedType
        {
            BasedOn = new Reference<ClassDef> { Target = gregorianDate },
        };
        var xmlTime = new FormattedType
        {
            BasedOn = new Reference<ClassDef> { Target = utc },
        };
        var xmlDateTime = new FormattedType
        {
            Extends = new Reference<TypeDef> { Target = xmlDate },
            BasedOn = new Reference<ClassDef> { Target = gregorianDateTime },
        };

        InternalInterlisModel = new ModelDef
        {
            Name = "INTERLIS",
            Content =
            {
                { "ANYUNIT", new UnitDef { Name = "ANYUNIT", Term = "ANYUNIT", Properties = { Property.Abstract } } },
                { "DIMENSIONLESS", new UnitDef { Name = "DIMENSIONLESS", Term = "DIMENSIONLESS", Properties = { Property.Abstract } } },
                { "LENGTH", unitLength },
                { "MASS", unitMass },
                { "TIME", unitTime },
                { "ELECTRIC_CURRENT", unitElectricCurrent },
                { "TEMPERATURE", unitTemperature },
                { "AMOUNT_OF_MATTER", unitAmountOfMatter },
                { "ANGLE", unitAngle },
                { "SOLID_ANGLE", unitSolidAngle },
                { "LUMINOUS_INTENSITY", unitLuminousIntensity },
                { "MONEY", new UnitDef { Name = "MONEY", Term = "MONEY", Properties = { Property.Abstract } } },

                { "m", new UnitDef { Name = "m", Term = "METER", Extends = new Reference<UnitDef> { Target = unitLength } } },
                { "kg", new UnitDef { Name = "kg", Term = "KILOGRAM", Extends = new Reference<UnitDef> { Target = unitMass } } },
                { "s", new UnitDef { Name = "s", Term = "SECOND", Extends = new Reference<UnitDef> { Target = unitTime } } },
                { "A", new UnitDef { Name = "A", Term = "AMPERE", Extends = new Reference<UnitDef> { Target = unitElectricCurrent } } },
                { "K", new UnitDef { Name = "K", Term = "DEGREE_KELVIN", Extends = new Reference<UnitDef> { Target = unitTemperature } } },
                { "mol", new UnitDef { Name = "mol", Term = "MOLE", Extends = new Reference<UnitDef> { Target = unitAmountOfMatter } } },
                { "rad", new UnitDef { Name = "rad", Term = "RADIAN", Extends = new Reference<UnitDef> { Target = unitAngle } } },
                { "sr", new UnitDef { Name = "sr", Term = "STERADIAN", Extends = new Reference<UnitDef> { Target = unitSolidAngle } } },
                { "cd", new UnitDef { Name = "cd", Term = "CANDELA", Extends = new Reference<UnitDef> { Target = unitLuminousIntensity } } },
                {
                    "URI",
                    new DomainDef
                    {
                        Name = "URI",
                        Properties = { Property.Final },
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
                        Properties = { Property.Final },
                        TypeDef = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            Length = 255,
                        },
                    }
                },
                {
                    "INTERLIS_1_DATE",
                    new DomainDef
                    {
                        Name = "INTERLIS_1_DATE",
                        Properties = { Property.Final },
                        TypeDef = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            Length = 8,
                        },
                    }
                },
                {
                    "BOOLEAN",
                    new DomainDef
                    {
                        Name = "BOOLEAN",
                        Properties = { Property.Final },
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
                        Properties = { Property.Final },
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
                        Properties = { Property.Final },
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
                { "min", new UnitDef { Name = "min", Term = "Minute" } },
                { "h", new UnitDef { Name = "h", Term = "Hour" } },
                { "d", new UnitDef { Name = "d", Term = "Day" } },
                { "M", new UnitDef { Name = "M", Term = "Month" } },
                { "Y", new UnitDef { Name = "Y", Term = "Year" } },
                { "TimeOfDay", timeOfDay },
                { "UTC", utc },
                { "GregorianDate", gregorianDate },
                { "GregorianDateTime", gregorianDateTime },
                { "XMLDate", new DomainDef { Name = "XMLDate", TypeDef = xmlDate } },
                { "XMLTime", new DomainDef { Name = "XMLTime", TypeDef = xmlTime } },
                { "XMLDateTime", new DomainDef { Name = "XMLDateTime", TypeDef = xmlDateTime } },
            }
        };
    }

    /// <summary>
    /// Resolves the reference. If successful, the <see cref="Reference{T}.Target"/> is set accordingly.
    /// </summary>
    private void Resolve(IUnresolvedReference reference)
    {
        if (reference == null || reference.Source == null)
        {
            return;
        }

        if (reference.Path.Count == 0)
        {
            throw new ArgumentException("Empty reference", nameof(reference.Path));
        }

        List<IInterlisDefinition> potentialTargets = new List<IInterlisDefinition>();

        // Search root model and resolve relative
        IInterlisDefinitionContainer? current = reference.Source;
        IInterlisDefinitionContainer root = reference.Source;
        while (current != null)
        {
            root = current;
            if (current.Content.TryGetValue(reference.Path[0], out var element))
            {
                // If the reference is longer than 1 it must be fully qualified and is resolved later from the root
                if (reference.Path.Count == 1)
                {
                    // Found inside current model
                    potentialTargets.Add(element);
                }
            }

            current = current?.Parent;
        }

        // At the root is always a model if a complete interlis model was parsed
        if (root is ModelDef model)
        {
            if (reference.Path[0] == model.Name)
            {
                potentialTargets.AddIfNotNull(ResolveAbsolute(reference, model));
            }

            // search in imports fully qualified
            if (model.Imports.TryGetValue(reference.Path[0], out var importedModel))
            {
                potentialTargets.AddIfNotNull(ResolveAbsolute(reference, importedModel.ModelDef));
            }

            // search in imports unqualified
            if (reference.Path.Count == 1)
            {
                foreach (var unqualifiedImport in model.Imports.Values.Where(m => m.IsUnqualifiedAllowed))
                {
                    if (unqualifiedImport.ModelDef?.Content.TryGetValue(reference.Path[0], out var element) == true)
                    {
                        potentialTargets.Add(element);
                    }
                }
            }
        }

        var mappedTargets = potentialTargets
            .Where(reference.CanAccept)
            .ToList();

        switch (mappedTargets.Count)
        {
            case 0:
                logger.LogError("Could not resolve '{Reference}'", reference);
                break;

            case 1:
                reference.SetTarget(mappedTargets.Single());
                break;

            default:
                logger.LogError("Ambiguous '{Reference}' could be resolved to multiple targets: {Targets}", reference, string.Join(", ", mappedTargets.Select(d => d.FullyQualifiedName)));
                break;
        }
    }

    private IInterlisDefinition? ResolveAbsolute(IUnresolvedReference reference, ModelDef? root)
    {
        IInterlisDefinition? target = root;
        for (var i = 1; i < reference.Path.Count; i++)
        {
            if (!(target is IContainer<IInterlisDefinition> collectionTarget && collectionTarget.Content.TryGetValue(reference.Path[i], out target)))
            {
                return null;
            }
        }

        return target;
    }

    public override InterlisEnvironment VisitInterlisEnvironment([NotNull] InterlisEnvironment interlisEnvironment)
    {
        // resolve model imports
        var modelDefs = interlisEnvironment.Content.Values.OfType<ModelDef>().ToList();
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
                else
                {
                    logger.LogError("Could not resolve import '{Import}' in model '{Model}'", import.Key, model.Name);
                }
            }
        }

        // resolve references
        foreach (var reference in referencesToResolve)
        {
            Resolve(reference);
        }

        // visit children
        base.VisitInterlisEnvironment(interlisEnvironment);

        return interlisEnvironment;
    }

    public override object? VisitAttributeDef([NotNull] AttributeDef attributeDef)
    {
        // Add references from classDefs to the associations they are part of.
        if (attributeDef.TypeDef is RoleType roleType && attributeDef.Parent is AssociationDef association)
        {
            foreach (var target in roleType.Targets)
            {
                if (target.Value?.Target is IIdentifiable classOrAssociationDef)
                {
                    classOrAssociationDef.AssociationAccess.TryAdd(association.Name, association);
                }
            }
        }

        return base.VisitAttributeDef(attributeDef);
    }
}
