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

    /// <summary>
    /// The filepath or URL of the document the range lies in, or <see langword="null"/> if unknown.
    /// </summary>
    public string? SourceUri { get; init; }

    public RangePosition() { }

    /// <summary>
    /// Convenience constructor that automatically creates new Position objects for start and end positions.
    /// </summary>
    [SetsRequiredMembers]
    public RangePosition(int startLine, int startCharacter, int endLine, int endCharacter, string? sourceUri = null)
    {
        Start = new Position { Line = startLine, Character = startCharacter };
        End = new Position { Line = endLine, Character = endCharacter };
        SourceUri = sourceUri;
    }

    /// <summary>
    /// Renders the range as <c>line:character-line:character</c> with one-based lines and zero-based characters, the
    /// convention of the compiler's <c>Compile error at ...</c> messages. The <see cref="SourceUri"/> is not included.
    /// </summary>
    public override string ToString() => $"{Start.Line + 1}:{Start.Character}-{End.Line + 1}:{End.Character}";
}
