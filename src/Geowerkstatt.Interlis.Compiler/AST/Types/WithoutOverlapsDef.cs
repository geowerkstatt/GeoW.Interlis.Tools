namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// A line type's <c>WITHOUT OVERLAPS [ '&gt;' Dec ]</c> declaration (RefHB 3.8.12.2-32): requests overlap-freedom
/// of the line's segments, written with an explicit tolerance (<see cref="Explicit"/>) or without one
/// (<see cref="Implicit"/>). <see cref="ILineType.WithoutOverlaps"/> is <see langword="null"/> when the clause is
/// not written — which for a <c>POLYLINE</c> means overlap-freedom is not demanded (RefHB 3.8.12.2-19), while for
/// surfaces and area partitions it is mandatory anyway and only the implicit or inherited tolerance applies
/// (RefHB 3.8.12.2-22, 3.8.13.1-13).
/// </summary>
public abstract class WithoutOverlapsDef
{
    /// <summary>
    /// The clause written without a tolerance (<c>WITHOUT OVERLAPS</c>): the implicit tolerance — the arc track
    /// width derived from the vertex coordinates' precision — or the inherited one applies (RefHB 3.8.13.1-13).
    /// </summary>
    public sealed class Implicit : WithoutOverlapsDef
    {
    }

    /// <summary>
    /// The clause written with an explicit tolerance (<c>WITHOUT OVERLAPS &gt; 0.005</c>): the arrow height below
    /// which overlapping arc segments are accepted (RefHB 3.8.12.2-16/-20).
    /// </summary>
    public sealed class Explicit : WithoutOverlapsDef
    {
        public required double Tolerance { get; init; }
    }
}
