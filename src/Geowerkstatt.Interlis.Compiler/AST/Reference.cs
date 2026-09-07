namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A reference to another element of the model. Usually the target is an <see cref="IInterlisDefinition"/> and the
/// reference is registered for scoped resolution; a target outside the definition world (e.g. a
/// <see cref="MetaObjectDeclaration"/>) rides the same carrier unregistered, with its <see cref="Target"/> written
/// by the pass that owns the lookup.
/// </summary>
/// <typeparam name="T">The type of the target.</typeparam>
public class Reference<T> : IReference where T : class, IReferenceTarget
{
    /// <summary>
    /// The resolved object.
    /// </summary>
    public T? Target { get; set; }

    /// <inheritdoc />
    IReferenceTarget? IReference.Target => Target;

    /// <inheritdoc />
    public IInterlisDefinitionContainer? Source { get; init; }

    /// <summary>
    /// A function that maps the target object to the desired type or returns <c>null</c> if the given <see cref="IInterlisDefinition"/> is not applicable.
    /// </summary>
    public Func<IInterlisDefinition, T?> MapTarget { get; init; } = element => element as T;

    /// <summary>
    /// Whether the <see cref="Path"/> denotes a model name (an import or a <c>TRANSLATION OF</c> clause,
    /// RefHB 3.5.1). Such references are resolved against the models of the environment instead of the
    /// lexical scopes, because the named model is a sibling of the referencing model, not part of it.
    /// </summary>
    public bool ResolvesInEnvironment { get; init; }

    /// <inheritdoc />
    public List<string> Path { get; } = new List<string>();

    /// <summary>
    /// The span of the reference's path in the INTERLIS source file.
    /// </summary>
    public RangePosition? SourceRange { get; init; }

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
        return $"reference '{(Path.Any() ? string.Join(".", Path) : (Target as IInterlisDefinition)?.FullyQualifiedName)}'{(Source == null ? "" : " from " + Source.FullyQualifiedName)}";
    }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitReference(this);
    }
}
