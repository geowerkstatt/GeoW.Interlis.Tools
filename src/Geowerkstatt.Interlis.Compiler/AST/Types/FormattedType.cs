namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class FormattedType : TypeDef
{
    public Reference<ClassDef>? BasedOn { get; set; }
    public Reference<FormattedType>? FormatBaseType { get; set; }
    public string? Min { get; init; }
    public string? Max { get; init; }
}
