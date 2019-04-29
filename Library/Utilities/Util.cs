using System.Collections.Generic;
using System.Linq;

namespace ApolloLensLibrary.Utilities
{
    public static class Util
    {
        /// <summary>
        /// Allows for cleaner range based for loop.
        /// Not necessarily good performance for large
        /// values.
        /// Inspired by Python's for i in Range(x):
        /// </summary>
        /// <example>
        /// for (var i in Util.Range(10))
        /// { }
        /// </example>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<int> Range(int count)
        {
            return Enumerable.Range(0, count);
        }
    }
}
