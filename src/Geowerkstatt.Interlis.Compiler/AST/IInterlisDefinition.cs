namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An INTERLIS object that can be referenced by its fully qualified name.
/// </summary>
public interface IInterlisDefinition : IAstElement
{
    /// <summary>
    /// The name of this element.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The Locations of the <see cref="Name"/> in the INTERLIS source file.
    /// </summary>
    public ICollection<RangePosition> NameLocations { get; }

    /// <summary>
    /// The parent <see cref="IInterlisDefinitionContainer"/> or <c>null</c> if this definition has no parent.
    /// </summary>
    public IInterlisDefinitionContainer? Parent { get; set; }

    /// <summary>
    /// The fully qualified name
    /// </summary>
    public string FullyQualifiedName => Parent != null ? $"{Parent.FullyQualifiedName}.{Name}" : Name;
}
