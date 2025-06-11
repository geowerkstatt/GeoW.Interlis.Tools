namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class EnumerationValuesList : List<EnumerationTreeNode>
{
    public bool IsFinal { get; set; }

    public EnumerationValuesList()
    {
    }

    public EnumerationValuesList(bool isFinal)
    {
        IsFinal = isFinal;
    }

    /// <summary>
    /// Add all elements of the <paramref name="other"/> <see cref="EnumerationValuesList"/> and update <see cref="IsFinal"/>.
    /// </summary>
    /// <remarks>This method is called <c>Add</c> so it can be used in collection initializer expressions.</remarks>
    public void Add(EnumerationValuesList other)
    {
        IsFinal = IsFinal || other.IsFinal;
        AddRange(other);
    }
}
