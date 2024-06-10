using System.Xml;
using System.Xml.Linq;

namespace Geowerkstatt.Interlis.Tools.XtfReader;

internal static class XmlReaderExtensions
{
    /// <summary>
    /// Check whether the <paramref name="reader"/> is at the expected element. Otherwise throw an exception.
    /// </summary>
    /// <exception cref="ArgumentException">The <paramref name="reader"/> was not at the expected element.</exception>
    public static void EnsureIsStartElement(this XmlReader reader, string localName, string namespaceURI)
    {
        if (!reader.IsStartElement(localName, namespaceURI))
        {
            var position = string.Empty;
            if (reader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                position = $" at line {lineInfo.LineNumber}:{lineInfo.LinePosition}";
            }

            throw new ArgumentException($"Expected element {namespaceURI}:{localName} but got {reader.NamespaceURI}:{reader.LocalName}{position}.");
        }
    }

    /// <summary>
    /// Advances the <see cref="XtfReader"/> to the next sibling element.
    /// </summary>
    public static bool ReadToNextSibling(this XmlReader reader)
    {
        var startDepth = reader.Depth;
        while(reader.Read() && reader.Depth > startDepth)
        {
        }

        while(!reader.EOF && reader.Depth >= startDepth && reader.NodeType != XmlNodeType.Element)
        {
            reader.Read();
        }

        return reader.Depth == startDepth;
    }

    /// <summary>
    /// Advances the <see cref="XmlReader"/> to the next descendant element.
    /// </summary>
    public static bool ReadToDescendant(this XmlReader reader)
    {
        var startDepth = reader.Depth;
        while (reader.Read() && reader.Depth > startDepth && reader.NodeType != XmlNodeType.Element)
        {
        }

        return reader.Depth > startDepth;
    }

    public static IEnumerable<XElement> WhereName(this IEnumerable<XElement> elements, string @namespace, string localName)
    {
        return elements.Where(e => e.Name.NamespaceName == @namespace && e.Name.LocalName == localName);
    }

    public static IEnumerable<XAttribute> WhereName(this IEnumerable<XAttribute> elements, string @namespace, string localName)
    {
        return elements.Where(e => e.Name.NamespaceName == @namespace && e.Name.LocalName == localName);
    }
}
