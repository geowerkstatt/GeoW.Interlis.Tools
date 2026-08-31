namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An INTERLIS object that can be referenced by its fully qualified name. Its <see cref="IReferenceTarget.Name"/>
/// and <see cref="IReferenceTarget.NameLocations"/> come from <see cref="IReferenceTarget"/>; every definition can
/// carry doc-comments and meta-attributes (see <see cref="IDocumentation"/>) and has a source range (see
/// <see cref="ISourceRange"/>).
/// </summary>
public interface IInterlisDefinition : IVisitable, IReferenceTarget, IDocumentation, ISourceRange
{
    /// <summary>
    /// The parent <see cref="IInterlisDefinitionContainer"/> or <c>null</c> if this definition has no parent.
    /// </summary>
    public IInterlisDefinitionContainer? Parent { get; set; }

    /// <summary>
    /// The base-language element this element is a translation of, or <c>null</c> if it is not part of a
    /// translated model (RefHB 3.5.1-10). On a <see cref="ModelDef"/> this is the authored <c>TRANSLATION OF</c>
    /// reference, registered and resolved by name against the sibling models of the environment. On every other
    /// definition the reference is unregistered and the reference resolver writes the target: a translation may
    /// only change names, so each element corresponds to the element of the original model at the same declaration
    /// position, which always has the same kind as this element.
    /// </summary>
    public Reference<IInterlisDefinition>? TranslationOf { get; set; }

    /// <summary>
    /// The fully qualified name
    /// </summary>
    public string FullyQualifiedName => Parent != null ? $"{Parent.FullyQualifiedName}.{Name}" : Name;
}
