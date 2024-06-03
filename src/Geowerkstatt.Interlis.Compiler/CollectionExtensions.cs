using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools;

internal static class CollectionExtensions
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
}
