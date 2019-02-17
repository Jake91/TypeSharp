using System.Collections.Generic;

namespace TypeSharp
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            if (source is List<T> listSource)
            {
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
