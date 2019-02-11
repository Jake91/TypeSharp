using System;
using System.Collections.Generic;
using System.Text;

namespace TypeSharp
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            if (source is List<T> listSource)
            {
                // do native AddRange if possible (better performance)
                listSource.AddRange(items);
                return;
            }
            foreach (var item in items)
            {
                source.Add(item);
            }
        }
    }
}
