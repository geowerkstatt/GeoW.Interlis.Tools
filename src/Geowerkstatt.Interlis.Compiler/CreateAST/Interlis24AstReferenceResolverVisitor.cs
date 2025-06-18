using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Resolves various references inside the AST. The AST is modified in place.
/// </summary>
public class Interlis24AstReferenceResolverVisitor(ILoggerFactory loggerFactory) : Interlis24AstBaseVisitor<bool>
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Interlis24AstReferenceResolverVisitor>();
    private readonly Scope<InterlisEnvironment> currentEnvironment = new();

    /// <summary>
    /// Resolves the reference. If successful, the <see cref="Reference{T}.Target"/> is set accordingly.
    /// </summary>
    /// <returns><see langword="true"/> if the <paramref name="reference"/> was resolved successfully, <see langword="false"/> otherwise.</returns>
    private bool Resolve<T>(Reference<T> reference) where T : class, IInterlisDefinition
    {
        if (reference.Target != null)
        {
            // Already resolved
            return true;
        }

        if (reference == null || reference.Source == null)
        {
            // Nothing to resolve
            return true;
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
            if (model.Imports.TryGetValue(reference.Path[0], out var import))
            {
                // import already resolved
                if (import.ModelDef.Target != null)
                {
                    potentialTargets.AddIfNotNull(ResolveAbsolute(reference, import.ModelDef.Target));
                }
                // a fully qualified path with 1 element references a model (used in import statements)
                else if (reference.Path.Count == 1 && currentEnvironment.Value != null)
                {
                    // resolve model import
                    potentialTargets.AddIfNotNull(currentEnvironment.Value.Content.GetValueOrDefault(reference.Path[0]));
                }
            }

            // search in imports unqualified
            if (reference.Path.Count == 1)
            {
                foreach (var unqualifiedImport in model.Imports.Values.Where(m => m.IsUnqualifiedAllowed))
                {
                    if (unqualifiedImport.ModelDef?.Target?.Content.TryGetValue(reference.Path[0], out var element) == true)
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
                return false;

            case 1:
                reference.SetTarget(mappedTargets.Single());
                return true;

            default:
                logger.LogError("Ambiguous '{Reference}' could be resolved to multiple targets: {Targets}", reference, string.Join(", ", mappedTargets.Select(d => d.FullyQualifiedName)));
                return false;
        }
    }

    private IInterlisDefinition? ResolveAbsolute(IReference reference, ModelDef? root)
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

    protected internal override bool DefaultResult => true;

    protected internal override bool AggregateResult(bool aggregate, bool nextResult)
    {
        return aggregate && nextResult;
    }

    public override bool VisitInterlisEnvironment([NotNull] InterlisEnvironment interlisEnvironment)
    {
        using var scopeFrame = currentEnvironment.NewFrame(interlisEnvironment);
        return base.VisitInterlisEnvironment(interlisEnvironment);
    }

    public override bool VisitAttributeDef([NotNull] AttributeDef attributeDef)
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

    public override bool VisitReference<T>([NotNull] Reference<T> reference)
    {
        return Resolve(reference);
    }
}
