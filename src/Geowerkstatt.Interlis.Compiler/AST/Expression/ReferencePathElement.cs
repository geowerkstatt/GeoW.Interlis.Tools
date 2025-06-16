namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class ReferencePathElement : IPathElement
{
    public required Reference<IInterlisDefinition> Value { get; init; }
}
