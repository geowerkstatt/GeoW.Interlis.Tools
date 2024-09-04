using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

/// <summary>
/// Defines how many objects are applicable and if they are ordered.
/// </summary>
public sealed record Cardinality
{
    public static readonly long? Unbound = null;

    public required long? Min { get; init; } = Unbound;
    public required long? Max { get; init; } = Unbound;
    public bool Ordered { get; init; } = false;
    public RelationshipType Type { get; init; } = RelationshipType.Association;

    public enum RelationshipType
    {
        /// <summary>
        /// Loose connection
        /// </summary>
        Association = Interlis24Parser.ASSOCIATION_SYMBOL,

        /// <summary>
        /// Feeble relationship between the entirety and its parts.
        /// </summary>
        Aggregation = Interlis24Parser.AGGREGATION_SYMBOL,

        /// <summary>
        /// Strong relationship between the entirety and its parts.
        /// </summary>
        Composition = Interlis24Parser.COMPOSITION_SYMBOL,
    }
}
