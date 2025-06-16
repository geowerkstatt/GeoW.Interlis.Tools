namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A range in a text document expressed as (zero-based) start and end positions.
/// </summary>
public class RangePosition
{
    /// <summary>
    /// The range's start position.
    /// </summary>
    public required Position Start { get; init; }

    /// <summary>
    /// The range's end position.
    /// </summary>
    public required Position End { get; init; }
}
