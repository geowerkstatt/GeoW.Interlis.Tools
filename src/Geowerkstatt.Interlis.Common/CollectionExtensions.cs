namespace Geowerkstatt.Interlis.Common;

public static class CollectionExtensions
{
    /// <summary>
    /// Add all <paramref name="items"/> one by one to the <paramref name="collection"/>.
    /// </summary>
    /// <remarks>This extension method can be used in object initializers.</remarks>
    public static void Add<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Add the <paramref name="element"/> to the <paramref name="collection"/> if it is not null.
    /// </summary>
    public static void AddIfNotNull<T>(this ICollection<T> collection, T? element) where T : class
    {
        if (element != null)
        {
            collection.Add(element);
        }
    }

    /// <summary>
    /// Filters out null values in a way that makes the C# compiler happy.
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> collection) where T : class
    {
        return collection.Where(e => e != null)!;
    }
}
