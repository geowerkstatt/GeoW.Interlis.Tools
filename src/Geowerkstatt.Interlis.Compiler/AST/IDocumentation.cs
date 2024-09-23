namespace Geowerkstatt.Interlis.Tools.AST;

/// <summary>
/// An INTERLIS object that can have Meta-Attributes and Doc-Comments.
/// </summary>
public interface IDocumentation
{
    IList<string> DocComments { get; }
    IDictionary<string, string> MetaAttributes { get; }
}
