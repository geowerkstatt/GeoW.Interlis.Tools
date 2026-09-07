namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class EnumerationValuesList : List<EnumerationTreeNode>
{
    /// <summary>
    /// Whether the enumeration is frozen (<c>FINAL</c>, RefHB 3.8.2-14/-19): no extension may add elements at
    /// this nesting. An EMPTY final list is the bare <c>(FINAL)</c> freeze form — the grammar's list alternative
    /// always carries at least one element, so the two source forms stay distinguishable by <see cref="List{T}.Count"/>;
    /// an empty final list is meaningful and must not be normalized away.
    /// </summary>
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
