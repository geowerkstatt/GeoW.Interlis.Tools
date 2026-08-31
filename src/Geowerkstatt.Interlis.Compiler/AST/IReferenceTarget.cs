namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An element a <see cref="Reference{T}"/> may point at. It carries a name and the source locations that name
/// occupies, which is all a resolved reference needs for diagnostics and for navigation. Implemented by every
/// <see cref="IInterlisDefinition"/> and by the targets outside the definition world that ride the same carrier
/// without taking part in scoped resolution (see <see cref="MetaObjectDeclaration"/>).
/// </summary>
public interface IReferenceTarget
{
    /// <summary>
    /// The name of this element.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The locations of the <see cref="Name"/> in the INTERLIS source file.
    /// </summary>
    public ICollection<RangePosition> NameLocations { get; }
}
