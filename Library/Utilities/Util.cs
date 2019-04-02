using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
