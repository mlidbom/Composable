using System;
using System.Collections.Generic;

namespace Composable.SystemCE.LinqCE
{
    /// <summary/>
    public static partial class EnumerableCE
    {
        /// <summary>
        /// Represents a sequence first yielding <see cref="StartValue"/> and then infinitely yielding the last value plus <see cref="StepSize"/>
        /// </summary>
        public readonly struct IterationSpecification : IEquatable<IterationSpecification>
        {
            internal IterationSpecification(int startValue, int stepSize)
            {
                StartValue = startValue;
                StepSize = stepSize;
            }
            /// <summary/>
            internal readonly int StartValue;

            /// <summary/>
            internal readonly int StepSize;

            public bool Equals(IterationSpecification other) => StartValue == other.StartValue && StepSize == other.StepSize;
            public override bool Equals(object? obj) => obj is IterationSpecification other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(StartValue, StepSize);
            public static bool operator ==(IterationSpecification left, IterationSpecification right) => left.Equals(right);
            public static bool operator !=(IterationSpecification left, IterationSpecification right) => !left.Equals(right);
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