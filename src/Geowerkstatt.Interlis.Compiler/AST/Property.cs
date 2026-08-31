namespace Geowerkstatt.Interlis.Compiler.AST;

public enum Property
{
    Abstract = Interlis24Parser.ABSTRACT,
    Extended = Interlis24Parser.EXTENDED,
    Generic = Interlis24Parser.GENERIC,
    Final = Interlis24Parser.FINAL,
    Transient = Interlis24Parser.TRANSIENT,
    External = Interlis24Parser.EXTERNAL,
    Oid = Interlis24Parser.OID,
    Hiding = Interlis24Parser.HIDING,

    /// <summary>
    /// Parsed on association roles but never stored on a definition: the visitor folds it onto the role's
    /// population <see cref="Cardinality"/> (RefHB 3.7.4), which is orderedness's single home — the same one a
    /// <c>LIST</c>'s element cardinality uses.
    /// </summary>
    Ordered = Interlis24Parser.ORDERED,
}
