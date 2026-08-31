namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A topic-level block of constraints attached to a viewable from outside its definition
/// (<c>CONSTRAINTS OF ViewableRef = ... END;</c>, RefHB 3.12-41). The block is unnamed in source, so its inherited
/// <see cref="InterlisDefinition.Name"/> is synthesized (e.g. <c>CONSTRAINTS OF C #1</c>) — the spaces and <c>#</c>
/// make it impossible as a user identifier, so it never collides in the topic namespace.
/// It is an <see cref="IInterlisDefinitionContainer"/> so the constraints it carries are parented to the block
/// rather than directly to the enclosing topic; its <see cref="Content"/> stays empty because constraints live in
/// the <see cref="Constraints"/> list (see <see cref="IConstraintContainer"/>), not in the namespace.
/// </summary>
public sealed class ConstraintsBlockDef : InterlisDefinition, IInterlisDefinitionContainer, IConstraintContainer
{

    /// <summary>
    /// The viewable (class/association) the constraints are attached to (<c>CONSTRAINTS OF ViewableRef</c>).
    /// The reference is resolved during reference resolution; the resolved target's constraint counter is then
    /// continued to assign the <see cref="ConstraintDef.NameIndex"/> of the constraints in this block.
    /// </summary>
    public Reference<IInterlisDefinition>? Target { get; set; }

    public List<ConstraintDef> Constraints { get; } = new List<ConstraintDef>();

    /// <summary>
    /// Always empty: a <c>CONSTRAINTS OF</c> block declares no named members. Present only to satisfy
    /// <see cref="IInterlisDefinitionContainer"/>; the constraints are held in <see cref="Constraints"/>.
    /// </summary>
    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public ICollection<IReference> ContainerReferences { get; } = new List<IReference>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitConstraintsBlockDef(this);
    }
}
