namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// The rotation definition of a coordinate type (<c>ROTATION nullAxis -&gt; piHalfAxis</c>, RefHB 3.8.8).
/// Identifies which axes form the zero direction and the positive (pi/2) direction.
/// </summary>
public class RotationDef
{
    /// <summary>
    /// The 1-based index of the axis pointing in the zero direction.
    /// </summary>
    public required int NullAxis { get; init; }

    /// <summary>
    /// The 1-based index of the axis pointing in the positive (pi/2) direction.
    /// </summary>
    public required int PiHalfAxis { get; init; }
}
