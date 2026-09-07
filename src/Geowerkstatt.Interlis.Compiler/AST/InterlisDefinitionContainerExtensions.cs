namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Helpers for working with the elements of an <see cref="IInterlisDefinitionContainer"/>.
/// </summary>
public static class InterlisDefinitionContainerExtensions
{
    /// <summary>
    /// The elements of a container in the order they are declared in their source file, which is what the
    /// positional translation pairing (RefHB 3.5.1-10) needs. The order is taken from the elements'
    /// <see cref="ISourceRange"/> rather than from the enumeration order of <see cref="IContainer{T}.Content"/>,
    /// because a <see cref="Dictionary{TKey, TValue}"/> does not guarantee an enumeration order. An element
    /// without a source range (only a synthesized one, e.g. in <see cref="InternalModel"/>) sorts last; the sort
    /// is stable, so such elements keep their relative order.
    /// </summary>
    public static IEnumerable<IInterlisDefinition> InDeclarationOrder(this IInterlisDefinitionContainer container)
    {
        return container.Content.Values
            .OrderBy(element => element.SourceRange?.Start.Line ?? int.MaxValue)
            .ThenBy(element => element.SourceRange?.Start.Character ?? int.MaxValue);
    }
}
