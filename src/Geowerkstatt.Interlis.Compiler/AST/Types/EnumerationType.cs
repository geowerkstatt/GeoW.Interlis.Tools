namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class EnumerationType : TypeDef
{
    public Sequencings Sequencing { get; set; }

    public EnumerationValuesList Values { get; } = new EnumerationValuesList();

    public enum Sequencings
    {
        None,
        Ordered = Interlis24Parser.ORDERED,
        Circular = Interlis24Parser.CIRCULAR,
    }
}
