namespace Geowerkstatt.Interlis.Tools.AST;

/// <summary>
/// A reference to a <see cref="IInterlisDefinition"/> that is not yet resolved.
/// </summary>
public class UnresolvedReference
{
    /// <summary>
    /// An action that is called with the resolved element as an argument.
    /// </summary>
    public Action<IInterlisDefinition>? SetSource;

    /// <summary>
    /// The source of the reference.
    /// </summary>
    public IInterlisDefinitionContainer? Source;

    /// <summary>
    /// The path to the target.
    /// </summary>
    public List<string> Target { get; } = new List<string>();

    public override string ToString()
    {
        return $"reference '{string.Join(".", Target)}'{(Source == null ? "" : " from " + Source.FullyQualifiedName)}";
    }

    /// <summary>
    /// Tries to resolve this reference. If successful, the <see cref="SetSource"/> action is called with the resolved element.
    /// </summary>
    /// <returns><c>true</c> if the reference was resolved, <c>false</c> otherwise.</returns>
    public bool TryResolve()
    {
        if (Source == null)
        {
            throw new ArgumentNullException(nameof(Source));
        }

        if (Target.Count == 0)
        {
            throw new ArgumentException("Empty reference", nameof(Target));
        }

        // Search root model and resolve relative
        IInterlisDefinitionContainer? current = Source;
        IInterlisDefinitionContainer root = Source;
        while (current != null)
        {
            root = current;
            if (current.Content.TryGetValue(Target[0], out var element))
            {
                if (Target.Count == 1)
                {
                    // Found inside current modul
                    SetSource?.Invoke(element);
                    return true;
                }
                else
                {
                    // Reference must be fully qualified
                    return false;
                }
            }

            current = current?.Parent;
        }

        // At the root is always a model if a complete interlis model was parsed
        if (root is ModelDef model)
        {
            if (Target[0] == model.Name)
            {
                return ResolveAbsolute(model);
            }

            // search in imports fully qualified
            if (model.Imports.TryGetValue(Target[0], out var importedModel))
            {
                return ResolveAbsolute(importedModel.ModelDef);
            }

            // search in imports unqualified
            if (Target.Count == 1)
            {
                foreach (var unqualifiedImport in model.Imports.Values.Where(m => m.IsUnqualifiedAllowed))
                {
                    if (unqualifiedImport.ModelDef?.Content.TryGetValue(Target[0], out var element) == true)
                    {
                        SetSource?.Invoke(element);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool ResolveAbsolute(ModelDef? root)
    {
        IInterlisDefinition? target = root;
        for (var i = 1; i < Target.Count; i++)
        {
            if (!(target is IContainer<IInterlisDefinition> collectionTarget && collectionTarget.Content.TryGetValue(Target[i], out target)))
            {
                return false;
            }
        }

        if (target == null)
        {
            return false;
        }

        SetSource?.Invoke(target);
        return true;
    }
}
