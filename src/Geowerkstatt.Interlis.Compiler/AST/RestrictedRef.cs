namespace Geowerkstatt.Interlis.Compiler.AST;

public class RestrictedRef
{
    public Reference<IInterlisDefinition>? Value { get; set; }

    public List<Reference<IInterlisDefinition>> Restrictions { get; } = new List<Reference<IInterlisDefinition>>();
}
