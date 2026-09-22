namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A reference to another element of the model: a path of <see cref="PathSegment"/>s — the names as written, each
/// with its span and what it denotes — and the <see cref="Target"/> the path as a whole reaches. Which lookup
/// resolves it is its <see cref="Resolution"/>: a qualification looked up in the lexical scopes, a member of a
/// container the context establishes, or an object path navigated step by step (RefHB 3.13).
/// <para>
/// Every reference that stands for names written in the source is registered in its container's
/// <see cref="IInterlisDefinitionContainer.ContainerReferences"/> — whatever its resolution, and whether or not its
/// target is an <see cref="IInterlisDefinition"/> (a <see cref="MetaObjectDeclaration"/> rides the same carrier) —
/// so a consumer walking references sees every occurrence that navigation and rename have to cover.
/// </para>
/// <para>
/// A synthetic reference carries a <see cref="Target"/> for a link that was never written down (an implicit
/// <c>EXTENDS</c>, a translation link, the predefined <see cref="InternalModel"/>). It has no path and no source
/// range, nothing to navigate from, and stays unregistered.
/// </para>
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
    public List<PathSegment> Path { get; } = new List<PathSegment>();

    /// <inheritdoc />
    IReadOnlyList<PathSegment> IReference.Path => Path;

    /// <inheritdoc />
    public ReferenceResolution Resolution { get; init; } = ReferenceResolution.Scoped;

    /// <summary>
    /// The span of the whole path in the INTERLIS source file, derived from the segments: a single segment's own
    /// span, or from the start of the first segment to the end of the last. <see langword="null"/> when there are no
    /// segments, or when an outer one has no span — an implied path such as <c>INTERLIS.NOOID</c> behind
    /// <c>NO OID</c> was never written down. Inner segments do not matter: the span is a bracket, not a union.
    /// </summary>
    public RangePosition? SourceRange => Path switch
    {
        [{ Range: { } only }] => only,
        [{ Range: { } first }, .., { Range: { } last }] => new RangePosition { Start = first.Start, End = last.End, SourceUri = first.SourceUri },
        _ => null,
    };

    /// <inheritdoc />
    public bool CanAccept(IInterlisDefinition potentialTarget)
    {
        return MapTarget(potentialTarget) != null;
    }

    /// <inheritdoc />
    public void SetTarget(IInterlisDefinition target)
    {
        Target = MapTarget(target);
        if (Path is [.., var last] && last is not KeywordPathSegment)
        {
            last.Target = target;
        }
    }

    public override string ToString()
    {
        var separator = Resolution == ReferenceResolution.ObjectPath ? "->" : ".";
        return $"reference '{(Path.Any() ? string.Join(separator, Path) : (Target as IInterlisDefinition)?.FullyQualifiedName)}'{(Source == null ? "" : " from " + Source.FullyQualifiedName)}";
    }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitReference(this);
    }
}
