namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// The Position in a file.
/// </summary>
public class Position
{
    /// <summary>
    /// Line position in a document (zero-based).
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Character offset on a line in a document (zero-based).
    /// </summary>
    public required int Character { get; init; }
}
