using System.Collections.Generic;
using System.Linq;

namespace ApolloLensLibrary.Utilities
{
    public static class Util
    {
        public static IEnumerable<int> Range(int count)
        {
            return Enumerable.Range(0, count);
        }
    }
}
