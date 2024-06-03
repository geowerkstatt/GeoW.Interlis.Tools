using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public record Identifier
{
    public string? Model { get; init; }

    public string? Topic { get; init; }

    /// <summary>
    /// The name of the class, association, type etc.
    /// </summary>
    public string? Class { get; init; }

    /// <summary>
    /// The name of attributes, roles etc.
    /// </summary>
    public string? LeafElementName { get; init; }
}
