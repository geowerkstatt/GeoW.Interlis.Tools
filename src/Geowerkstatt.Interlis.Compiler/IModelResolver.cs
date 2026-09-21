namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// Supplies the INTERLIS source of imported models on demand, so <see cref="InterlisReader.ReadModelWithImportsAsync"/>
/// can load a model's transitive dependencies while compiling it.
/// </summary>
public interface IModelResolver
{
    /// <summary>
    /// Opens the source of the model named <paramref name="modelName"/> for the given INTERLIS
    /// <paramref name="languageVersion"/>, or <see langword="null"/> if the model is not available. The version
    /// disambiguates a model name that is published for several INTERLIS versions, so the resolver returns the source
    /// matching the importing model. The returned <c>Reader</c> is disposed by the caller; <c>SourceUri</c> is the
    /// filepath or URL the source came from (applied to the loaded models' source URI for diagnostics), or
    /// <see langword="null"/> if unknown.
    /// </summary>
    /// <param name="modelName">The name of the imported model to resolve.</param>
    /// <param name="languageVersion">The INTERLIS language version of the importing model (e.g. <c>2.4</c>), or <see langword="null"/> if unknown.</param>
    /// <param name="cancellationToken">Cancels the resolution, e.g. a download.</param>
    /// <returns>The model's source and its origin, or <see langword="null"/> if it cannot be resolved.</returns>
    ValueTask<(TextReader Reader, string? SourceUri)?> OpenModelAsync(string modelName, double? languageVersion, CancellationToken cancellationToken);
}
