using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Utilities
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action action)
        {
            foreach (var item in enumeration)
            {
                action();
            }
        }

        public static IEnumerable<int> Times(this int count)
        {
            return Enumerable.Range(0, count);
        }
    }
}
