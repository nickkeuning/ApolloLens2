using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Utilities
{
    static class TaskExtensions
    {
        public static void DoNotAwait(this Task task) { }
    }
}
