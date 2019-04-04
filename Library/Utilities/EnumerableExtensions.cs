using System.Collections.Generic;
using System.Linq;

namespace ApolloLensLibrary.Utilities
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<int> Times(this int count)
        {
            return Enumerable.Range(0, count);
        }
    }
}
