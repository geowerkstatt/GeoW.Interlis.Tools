namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class BlackboxType : TypeDef
{
    public required BlackboxTypeKind Kind { get; init; }

    public enum BlackboxTypeKind
    {
        /// <summary>
        /// A blackbox type that has XML content.
        /// </summary>
        Xml = Interlis24Parser.XML,

        /// <summary>
        /// A blackbox type that has binary content.
        /// </summary>
        Binary = Interlis24Parser.BINARY,
    }
}
