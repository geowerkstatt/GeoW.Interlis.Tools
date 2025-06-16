namespace Geowerkstatt.Interlis.Compiler.AST;

public interface IInterlisDefinitionContainer : IContainer<IInterlisDefinition>, IInterlisDefinition
{
    /// <summary>
    /// References defined in this container.
    /// </summary>
    /// <remarks>The references are scattered throughout the container. This collection is for easy access to all of them.</remarks>
    public ICollection<IReference> ContainerReferences { get; }
}
