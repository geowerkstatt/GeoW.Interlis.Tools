namespace Geowerkstatt.Interlis.Tools.AST.Types;

/// <summary>
/// A type that references (extends) another type without changing anything.
/// </summary>
public class TypeRef : ITypeDef
{
    public Cardinality? Cardinality { get; set; }
    public ITypeDef? Extends { get; set; }
}
