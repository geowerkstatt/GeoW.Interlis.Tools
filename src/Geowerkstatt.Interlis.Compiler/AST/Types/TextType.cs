namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class TextType : TypeDef
{
    public int? Length { get; set; }
    public bool IsMText { get; init; } = false;
}
