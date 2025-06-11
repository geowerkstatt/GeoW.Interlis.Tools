
namespace Geowerkstatt.Interlis.Compiler.AST.Types;

/// <summary>
/// An enumeration value that can have sub-enumerations
/// </summary>
public class EnumerationTreeNode : IDocumentation
{
    public required string Name { get; init; }

    public EnumerationValuesList SubValues { get; } = new EnumerationValuesList();

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();
}
