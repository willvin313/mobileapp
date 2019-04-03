using System.Collections.Generic;

namespace Toggl.Multivac.Extensions
{
    public static class CollectionExtensions
    {
        public static int LastIndex<T>(this ICollection<T> self)
            => self.Count - 1;

        public static int LastIndex<T>(this IReadOnlyCollection<T> self)
            => self.Count - 1;
    }
}
