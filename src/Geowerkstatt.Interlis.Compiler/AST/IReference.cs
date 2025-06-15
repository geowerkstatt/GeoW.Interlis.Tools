namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Interface to put <see cref="Reference{T}"/> instances into a collection.
/// Does not care about the type of the target and has all necessary info to resolve the reference.
/// </summary>
public interface IReference
{
    /// <summary>
    /// The path to the target object.
    /// </summary>
    public List<string> Path { get; }

    /// <summary>
    /// The source of the reference.
    /// </summary>
    public IInterlisDefinitionContainer? Source { get; init; }

    /// <summary>
    /// Check if the given <paramref name="potentialTarget"/> is acceptable for this reference.
    /// </summary>
    public bool CanAccept(IInterlisDefinition potentialTarget);

    /// <summary>
    /// Set the resolved target object.
    /// </summary>
    public void SetTarget(IInterlisDefinition target);
}
