using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    /// <summary/>
    public static class Number
    {
        /// <summary>
        /// generates a sequence of integers beginning with <paramref name="me"/> where each element is 
        /// <paramref name="stepsize"/> larger than the previous
        /// </summary>
        public static IEnumerable<int> By(this int me, int stepsize)
        {
            while (me < int.MaxValue)
            {
                yield return me;
                me += stepsize;
            }
        }


        /// <summary>
        /// generates a sequence of integers beginning with <paramref name="me"/> where each element is 
        /// the previous element plus one
        /// </summary>
        public static IEnumerable<int> Through(this int me, int guard)
        {
            while (me <= guard)
            {
                yield return me++;
            }
        }
    }
}