namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class KeyWordPathElement : IPathElement
{
    public required KeyWordPathElement.KeyWord Value { get; init; }

    public enum KeyWord
    {
        This = Interlis24Parser.THIS,
        ThisArea = Interlis24Parser.THISAREA,
        ThatArea = Interlis24Parser.THATAREA,
        Parent = Interlis24Parser.PARENT,
        Aggregates = Interlis24Parser.AGGREGATES,
    }
}
