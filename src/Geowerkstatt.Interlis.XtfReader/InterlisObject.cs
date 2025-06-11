using Geowerkstatt.Interlis.Compiler.AST;

namespace Geowerkstatt.Interlis.XtfReader;

/// <summary>
/// Represents an instance of an interlis definition.
/// </summary>
public class InterlisObject
{
    public IInterlisDefinition? Definition { get; set; }

    public string? Tid { get; set; }

    public Dictionary<string, object> Attributes { get; } = new Dictionary<string, object>();

    public InterlisBasket Basket { get; set; }
}
