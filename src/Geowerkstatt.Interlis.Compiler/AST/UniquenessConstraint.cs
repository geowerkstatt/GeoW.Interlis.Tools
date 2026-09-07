using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A <c>UNIQUE</c> constraint: the listed attribute paths must be unique, either globally / per basket
/// (<see cref="GlobalUnique"/>) or within a substructure (<see cref="Local"/>) (RefHB 3.12).
/// </summary>
public sealed class UniquenessConstraint : ConstraintDef
{
    /// <summary>
    /// Whether uniqueness applies per basket (<c>UNIQUE (BASKET)</c>).
    /// </summary>
    public bool IsBasket { get; set; }

    /// <summary>
    /// The optional <c>WHERE</c> pre-condition restricting the objects the uniqueness applies to.
    /// </summary>
    public IExpression? Where { get; set; }

    /// <summary>
    /// The attribute paths that must be unique together (global form). Empty for the local form.
    /// </summary>
    public List<PathExpression> GlobalUnique { get; } = new List<PathExpression>();

    /// <summary>
    /// The local-uniqueness specification (<c>(LOCAL) structureAttr -&gt; ... : attr, ...</c>), or
    /// <see langword="null"/> for the global form.
    /// </summary>
    public LocalUniqueness? Local { get; set; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitUniquenessConstraint(this);
    }
}

/// <summary>
/// The <c>(LOCAL)</c> form of a <see cref="UniquenessConstraint"/>: the named attributes must be unique within
/// each instance of the given substructure path (RefHB 3.12).
/// </summary>
public sealed class LocalUniqueness
{
    /// <summary>
    /// The substructure attribute path (<c>structureAttr -&gt; structureAttr ...</c>). The references are not
    /// registered — each step is a member of the previous substructure, not a scoped name — so the path resolver
    /// sets their targets while walking the members.
    /// </summary>
    public List<Reference<AttributeDef>> StructurePath { get; } = new List<Reference<AttributeDef>>();

    /// <summary>
    /// The attributes that must be unique within each substructure instance; resolved like
    /// <see cref="StructurePath"/>.
    /// </summary>
    public List<Reference<AttributeDef>> AttributeNames { get; } = new List<Reference<AttributeDef>>();
}
