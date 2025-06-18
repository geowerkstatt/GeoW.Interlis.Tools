using System.Diagnostics.CodeAnalysis;

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

    public RangePosition() { }

    /// <summary>
    /// Convenience constructor that automatically creates new Position objects for start and end positions.
    /// </summary>
    [SetsRequiredMembers]
    public RangePosition(int startLine, int startCharacter, int endLine, int endCharacter)
    {
        Start = new Position { Line = startLine, Character = startCharacter };
        End = new Position { Line = endLine, Character = endCharacter };
    }
}
