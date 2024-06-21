namespace Geowerkstatt.Interlis.Tools.AST.Types;

public class EnumerationType : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }

    public Sequencings Sequencing { get; set; }

    public EnumerationValuesList Values { get; } = new EnumerationValuesList();

    public enum Sequencings
    {
        None,
        Ordered = Interlis24Parser.ORDERED,
        Circular = Interlis24Parser.CIRCULAR,
    }
}
