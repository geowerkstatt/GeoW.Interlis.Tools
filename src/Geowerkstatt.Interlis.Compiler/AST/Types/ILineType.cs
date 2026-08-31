namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public interface ILineType
{
    public Reference<DomainDef>? VertexType { get; set; }

    /// <summary>The <c>WITHOUT OVERLAPS</c> declaration; <see langword="null"/> when the clause is not written.</summary>
    public WithoutOverlapsDef? WithoutOverlaps { get; set; }

    /// <summary>
    /// The admitted segment forms (<c>WITH (STRAIGHTS, ARCS, CustomForm, ...)</c>, RefHB 3.8.12.2-29) in written
    /// order, duplicates preserved; empty when the clause is omitted (the form is then inherited or the type is
    /// incomplete, RefHB 3.8-4 / 3.8.12.2-1). Every form is a reference to a <see cref="LineFormTypeDef"/>: the
    /// <c>STRAIGHTS</c>/<c>ARCS</c> keywords reference the predefined <c>INTERLIS</c> line forms — like the
    /// predefined domain keywords, RefHB 3.8.12.1 — and a custom name is a (possibly model-qualified) reference to
    /// a <c>LINE FORM</c> definition (RefHB 3.8.12.3).
    /// </summary>
    public List<Reference<LineFormTypeDef>> LineForms { get; }
}
