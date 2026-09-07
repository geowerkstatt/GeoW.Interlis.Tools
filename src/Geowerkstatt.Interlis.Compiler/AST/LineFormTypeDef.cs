namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A custom line-form type declaration (<c>LINE FORM name : lineStructure;</c>, RefHB 3.8.12). It binds a
/// line-form name to the structure that describes the geometry of that line form, so the name can be used in
/// a line type's <c>WITH (...)</c> list.
/// </summary>
public sealed class LineFormTypeDef : InterlisDefinition
{
    /// <summary>
    /// The line structure this line-form type is based on.
    /// </summary>
    public Reference<ClassDef>? Structure { get; set; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitLineFormTypeDef(this);
    }
}
