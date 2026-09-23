namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Interlis Objects that can have a well defined location in the source file.
/// </summary>
public interface ISourceRange
{
    /// <summary>
    /// The source range of the object in the source file.
    /// </summary>
    public RangePosition? SourceRange { get; }
}
