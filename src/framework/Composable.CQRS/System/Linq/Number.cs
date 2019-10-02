using System;
using System.Collections.Generic;

namespace Composable.System.Linq
{
    /// <summary/>
    static class Number
    {
        /// <summary>
        /// Represents a sequence first yielding <see cref="StartValue"/> and then infinitely yielding the last value plus <see cref="StepSize"/>
        /// </summary>
        public readonly struct IterationSpecification
        {
            public IterationSpecification(int startValue, int stepSize)
            {
                StartValue = startValue;
                StepSize = stepSize;
            }
            /// <summary/>
            public readonly int StartValue;

            /// <summary/>
            public readonly int StepSize;
        }

        /// <summary>
        /// generates a sequence of integers beginning with <paramref name="me"/> where each element is
        /// <paramref name="stepSize"/> larger than the previous
        /// </summary>
        public static IterationSpecification By(this int me, int stepSize) => new IterationSpecification(me, stepSize);

        /// <summary>
        /// generates a sequence of integers beginning with <paramref name="me"/> where each element is
        /// the previous element plus one that includes the upper bound <paramref name="guard"/> cref="guard"/>
        /// </summary>
        public static IEnumerable<int> Through(this int me, int guard)
        {
            while(me <= guard)
            {
                yield return me++;
            }
        }

        /// <summary>
        /// generates a sequence of integers beginning with <paramref name="me"/> where each element is
        /// the previous element plus one that excludes the upper bound <paramref name="guard"/>
        /// </summary>
        public static IEnumerable<int> Until(this int me, int guard) => me.Through(guard - 1);

        /// <summary>
        /// Returns as sequence that will yield all values to and including <paramref name="guard"/>
        /// </summary>
        public static IEnumerable<int> Through(this IterationSpecification me, int guard)
        {
            var current = me.StartValue;
            if(me.StepSize > 0)
            {
                while(current <= guard)
                {
                    yield return current;
                    current += me.StepSize;
                }
            }
            else
            {
                while(current >= guard)
                {
                    yield return current;
                    current += me.StepSize;
                }
            }
        }

        /// <summary>
        /// Returns as sequence that will yield all values to but excluding <paramref name="guard"/>
        /// </summary>
        public static IEnumerable<int> Until(this IterationSpecification me, int guard) => me.Through(guard - Math.Sign(me.StepSize));
    }
}