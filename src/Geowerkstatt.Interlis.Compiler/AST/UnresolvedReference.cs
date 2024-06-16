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
    /// <remarks>Required for relative references.</remarks>
    public IInterlisDefinition? Source;

    /// <summary>
    /// The path to the target.
    /// </summary>
    public List<string> Target { get; } = new List<string>();

    /// <summary>
    /// Whether this reference is relative or absolute.
    /// </summary>
    public required bool IsRelative;

    public override string ToString()
    {
        return $"{(IsRelative ? "relative" : "absolute")} reference '{string.Join(".", Target)}'{(Source == null ? "" : " from " + Source.FullyQualifiedName)}";
    }

    public bool TryResolve(IContainer<IInterlisDefinition> context)
    {
        if (IsRelative)
        {
            if (Source == null)
            {
                throw new ArgumentNullException(nameof(Source));
            }
            return ResolveRelative(Source);
        }
        else
        {
            return ResolveAbsolute(context);
        }
    }

    private bool ResolveAbsolute(IContainer<IInterlisDefinition> context)
    {
        IInterlisDefinition? target = null;
        for (int i = 0; i < Target.Count; i++)
        {
            // Try to resolve the next element
            if (!context.Content.TryGetValue(Target[i], out IInterlisDefinition? element))
            {
                return false;
            }

            target = element;

            // If it is not the last iteration try to cast the found element to a container
            if (i < Target.Count - 1)
            {
                if (target is IContainer<IInterlisDefinition> subContext)
                {
                    context = subContext;
                }
                else
                {
                    return false;
                }
            }
        }

        if (target == null)
        {
            return false;
        }

        SetSource?.Invoke(target);
        return true;
    }

    private bool ResolveRelative(IInterlisDefinition source)
    {
        if (Target.Count == 0)
        {
            return false;
        }

        IInterlisDefinition? target = source;
        while (target != null)
        {
            if (target is IContainer<IInterlisDefinition> container && container.Content.TryGetValue(Target[0], out IInterlisDefinition? element))
            {
                if (Target.Count == 1)
                {
                    SetSource?.Invoke(element);
                    return true;
                }

                if (ResolveAbsolute(container))
                {
                    return true;
                }
            }

            target = target?.Parent;
        }

        return false;
    }
}
