namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class FormattedType : TypeDef
{
    public Reference<ClassDef>? BasedOn { get; init; }
    public Reference<DomainDef>? FormatBaseType { get; init; }
    public string? Min { get; init; }
    public string? Max { get; init; }
}
