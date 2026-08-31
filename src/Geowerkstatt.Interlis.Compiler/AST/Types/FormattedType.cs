namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class FormattedType : TypeDef
{
    public Reference<ClassDef>? BasedOn { get; init; }
    public Reference<DomainDef>? FormatBaseType { get; init; }
    public string? Min { get; init; }
    public string? Max { get; init; }

    /// <summary>
    /// The format definition of a <c>FORMAT BASED ON</c> domain (RefHB 3.8.6-6); <see langword="null"/>
    /// for the <c>FORMAT DomainRef Min..Max</c> and plain <c>Min..Max</c> forms.
    /// </summary>
    public FormatDef? Format { get; init; }
}
