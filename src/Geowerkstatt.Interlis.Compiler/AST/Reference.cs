namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A reference to another Definition in the interlis file.
/// </summary>
/// <typeparam name="T">The type of the target.</typeparam>
public class Reference<T> : IReference where T : class
{
    /// <summary>
    /// The resolved object.
    /// </summary>
    public T? Target { get; set; }

    /// <inheritdoc />
    public IInterlisDefinitionContainer? Source { get; init; }

    /// <summary>
    /// A function that maps the target object to the desired type or returns <c>null</c> if the given <see cref="IInterlisDefinition"/> is not applicable.
    /// </summary>
    public Func<IInterlisDefinition, T?> MapTarget { get; init; } = element => element as T;

    /// <summary>
    /// A function that is called when the target is resolved.
    /// </summary>
    public Action<T>? OnResolved;

    /// <inheritdoc />
    public List<string> Path { get; } = new List<string>();

    /// <inheritdoc />
    public bool CanAccept(IInterlisDefinition potentialTarget)
    {
        return MapTarget(potentialTarget) != null;
    }

    /// <inheritdoc />
    public void SetTarget(IInterlisDefinition target)
    {
        Target = MapTarget(target);
        OnResolved?.Invoke(Target!);
    }

    public override string ToString()
    {
        return $"reference '{(Path.Any() ? string.Join(".", Path) : (Target as IInterlisDefinition)?.FullyQualifiedName)}'{(Source == null ? "" : " from " + Source.FullyQualifiedName)}";
    }
}
