namespace Geowerkstatt.Interlis.Tools.AST;

public class Reference<T> : IUnresolvedReference where T : class
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
    }

    public override string ToString()
    {
        return $"reference '{string.Join(".", Path)}'{(Source == null ? "" : " from " + Source.FullyQualifiedName)}";
    }
}
