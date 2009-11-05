using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    public static class Seq
    {
        public static IEnumerable<T> Create<T>(params T[] values)
        {
            return values;
        }       
    }
}