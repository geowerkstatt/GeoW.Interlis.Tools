namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A meta-data basket declaration (<c>SIGN BASKET</c> / <c>REFSYSTEM BASKET</c>, RefHB 3.10.1). It introduces a
/// basket name, the topic it conforms to, and (per class) the names of the meta-objects expected in it.
/// </summary>
public sealed class MetaDataBasketDef : InterlisDefinition, IExtending<MetaDataBasketDef>
{
    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    /// <summary>
    /// Whether the basket holds signatures (<c>SIGN</c>) or reference-system objects (<c>REFSYSTEM</c>).
    /// </summary>
    public BasketKind Kind { get; set; }

    /// <summary>
    /// The basket this basket extends (<c>EXTENDS</c>), if any.
    /// </summary>
    public Reference<MetaDataBasketDef>? Extends { get; set; }

    /// <summary>
    /// The topic this basket conforms to (after <c>~</c>).
    /// </summary>
    public Reference<TopicDef>? Topic { get; set; }

    /// <summary>
    /// The expected meta-objects, grouped per class (<c>OBJECTS OF Class : name, ...</c>).
    /// </summary>
    public List<MetaObjectsClause> Objects { get; } = new List<MetaObjectsClause>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitMetaDataBasketDef(this);
    }

    public enum BasketKind
    {
        Sign = Interlis24Parser.SIGN,
        Refsystem = Interlis24Parser.REFSYSTEM,
    }

}

/// <summary>
/// One <c>OBJECTS OF Class-Name : MetaObject-Name {, MetaObject-Name}</c> clause of a
/// <see cref="MetaDataBasketDef"/> (RefHB 3.10.1).
/// </summary>
public sealed class MetaObjectsClause
{
    /// <summary>
    /// The class the listed meta-objects are instances of. Not registered — the class lives in the basket's
    /// topic, not the enclosing scope — so the reference resolver links it against the topic's (inherited)
    /// content.
    /// </summary>
    public required Reference<ClassDef> Class { get; init; }

    /// <summary>
    /// The expected meta objects, by name.
    /// </summary>
    public List<MetaObjectDeclaration> MetaObjects { get; } = new List<MetaObjectDeclaration>();
}

/// <summary>
/// One meta-object name a basket declares (RefHB 3.10.1-2). A meta object is data, not a model definition — this
/// declared name is the nearest model-world anchor it has, so it is the target a <c>{basket.metaObject}</c>
/// reference resolves to (written by the reference resolver onto <see cref="Types.RefSys.MetaObjectRef.MetaObject"/>). It is an
/// <see cref="IReferenceTarget"/> but not an <see cref="IInterlisDefinition"/>: it can be pointed at, yet it takes
/// no part in scoped name resolution.
/// </summary>
public sealed class MetaObjectDeclaration : IReferenceTarget
{
    public required string Name { get; init; }

    /// <summary>The locations of the name in the basket declaration; empty for hand-built models.</summary>
    public ICollection<RangePosition> NameLocations { get; } = new List<RangePosition>();
}
