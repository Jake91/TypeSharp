using System.Collections;
using System.Collections.Generic;

namespace TypeSharp.Common
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
        
        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            if (source != null)
            {
                return !source.GetEnumerator().MoveNext();
            }
            return true;
        }
    }
}
