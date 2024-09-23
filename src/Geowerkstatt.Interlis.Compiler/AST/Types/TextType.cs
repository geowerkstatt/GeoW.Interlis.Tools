namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class TextType : TypeDef
{
    public int? Length { get; set; }
    public bool IsMText { get; init; } = false;
}
