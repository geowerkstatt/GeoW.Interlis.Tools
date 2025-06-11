namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An element that acts as a container for other elements.
/// </summary>
public interface IContainer<T> where T : IInterlisDefinition
{
    /// <summary>
    /// The content of the container. Mapping child name to child element.
    /// </summary>
    Dictionary<string, T> Content { get; }
}
