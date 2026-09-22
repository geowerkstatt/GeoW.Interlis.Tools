using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// One segment of a <see cref="Reference{T}.Path"/>: a name as written, the span it occupies and what it denotes.
/// In a scoped reference the segments are the dot-separated names of a qualification (<c>Model.Topic.Name</c>,
/// RefHB 3.5.4). In an object path (<see cref="ReferenceResolution.ObjectPath"/>, RefHB 3.13) they are the
/// arrow-separated steps, where a step can also carry an index (<see cref="IndexedPathSegment"/>) or an association
/// qualifier (<see cref="RolePathSegment"/>), or be a keyword (<see cref="KeywordPathSegment"/>).
/// <para>
/// The span belongs to the segment rather than to the path as a whole because a rename replaces one name: a class
/// reached as <c>Model.Topic.ClassA</c> must have only its last segment rewritten, leaving the qualification alone.
/// There is deliberately no implicit conversion from <see cref="string"/>: it would let <c>segment == "Name"</c>
/// compile and silently compare the wrong things.
/// </para>
/// </summary>
public class PathSegment
{
    // Declaring the convenience constructor below removes the implicit parameterless one; member-by-member
    // initialization and the derived kinds still need it.
    public PathSegment()
    {
    }

    /// <summary>
    /// Convenience constructor for a bare name, so a path can be written as <c>Path = { new("Model"), new("Topic") }</c>.
    /// </summary>
    [SetsRequiredMembers]
    public PathSegment(string name, RangePosition? range = null)
    {
        Name = name;
        Range = range;
    }

    /// <summary>The name as written.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// The span of the name in the source file, or <see langword="null"/> for a path that was never written down
    /// (a hand-built model, a synthesized reference).
    /// </summary>
    public RangePosition? Range { get; init; }

    /// <summary>
    /// What the name denotes, written by the pass that resolves the reference. For the last segment this is the
    /// reference's target; for a qualification segment it is the container the next name was looked up in — the
    /// model or topic (or the class of an attribute-path constant) as WRITTEN, which for an inherited member is the
    /// extending container the source names, not the declaring one the member lives in; for a step of an object
    /// path it is the member the step reached. A rename of a definition rewrites every segment that denotes it.
    /// <see langword="null"/> while unresolved, and always for a <see cref="KeywordPathSegment"/>, which denotes the
    /// context object rather than a definition.
    /// </summary>
    public IReferenceTarget? Target { get; set; }

    /// <summary>The segment as written; a derived kind includes its index or qualifier.</summary>
    public override string ToString() => Name;
}
