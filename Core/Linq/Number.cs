using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    /// <summary/>
    public static class Number
    {
        public struct IterationSpecification
        {
            public int StartValue;
            public int StepSize;
        }

        /// <summary>
        /// generates a sequence of integers beginning with <paramref name="me"/> where each element is 
        /// <paramref name="stepsize"/> larger than the previous
        /// </summary>
        public static IterationSpecification By(this int me, int stepsize)
        {
            return new IterationSpecification
                   {
                       StartValue = me,
                       StepSize = stepsize
                   };
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

        public static IEnumerable<int> Through(this IterationSpecification me, int guard)
        {
            int current = me.StartValue;
            if (me.StepSize > 0)
            {
                while (current <= guard)
                {
                    yield return current ;
                    current += me.StepSize;
                }
            }else
            {
                while (current >= guard)
                {
                    yield return current;
                    current += me.StepSize;
                }
            }
        }
    }
}