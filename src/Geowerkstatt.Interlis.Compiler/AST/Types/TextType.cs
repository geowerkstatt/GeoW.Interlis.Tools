namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class TextType : TypeDef
{
    /// <summary>
    /// The maximum length in characters; <see langword="null"/> for a bare <c>TEXT</c>/<c>MTEXT</c>, which
    /// declares an UNLIMITED length (RefHB 3.8.1-3) — unlike a bound-less <c>NUMERIC</c>, a length-less text is
    /// not an omitted definition part and inherits nothing.
    /// </summary>
    public int? Length { get; set; }

    public bool IsMText { get; init; } = false;
}
