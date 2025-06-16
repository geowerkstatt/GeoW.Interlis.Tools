namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class IdentifierPathElement : IPathElement
{
    public required string Value { get; init; }
}
