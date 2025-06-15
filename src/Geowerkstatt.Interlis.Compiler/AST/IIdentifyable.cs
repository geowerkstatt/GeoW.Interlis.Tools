namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Represents INTERLIS definitions that are identifiable by an OID during the transfer.
/// </summary>
public interface IIdentifiable
{
    /// <summary>
    /// The type of the object identifier.
    /// </summary>
    public Reference<DomainDef>? OidType { get; set; }

    /// <summary>
    /// A collection of <see cref="AssociationDef"/>s this object is mentioned in.
    /// </summary>
    public Dictionary<string, AssociationDef> AssociationAccess { get; }
}
