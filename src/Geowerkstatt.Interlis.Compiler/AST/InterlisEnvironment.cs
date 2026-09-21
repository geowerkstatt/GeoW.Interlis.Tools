namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Represents a collection of models belonging together.
/// For example models from the same Interlis file or models that depend on each other.
/// </summary>
public sealed class InterlisEnvironment : IVisitable, IContainer<ModelDef>
{
    public double? Version { get; init; }

    public Dictionary<string, ModelDef> Content { get; } = new Dictionary<string, ModelDef>();

    /// <summary>
    /// Merges the models of <paramref name="other"/> into this environment. Each model name is added at most once
    /// (the first definition wins), so a model imported by several files is included only once and the shared
    /// <see cref="InternalModel.Interlis"/> is de-duplicated. Both environments must be of the same INTERLIS
    /// <see cref="Version"/>: models of different versions do not belong together.
    /// </summary>
    /// <param name="other">The environment whose models are merged into this one.</param>
    /// <exception cref="InvalidOperationException"><paramref name="other"/> has a different <see cref="Version"/>.</exception>
    public void MergeFrom(InterlisEnvironment other)
    {
        if (other.Version != Version)
        {
            throw new InvalidOperationException($"Cannot merge an environment of INTERLIS version {other.Version?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<none>"} into one of version {Version?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<none>"}.");
        }

        foreach (var model in other.Content)
        {
            Content.TryAdd(model.Key, model.Value);
        }
    }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitInterlisEnvironment(this);
    }
}
