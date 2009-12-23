using System;

namespace Void.Time
{
    /// <summary>Operations on an <see cref="IDuration"/></summary>
    public static class Duration
    {
        /// <summary>The canonical instance of an <see cref="IDuration"/> with zero length in time.</summary>
        public static readonly IDuration Zero = FromTimeSpan(TimeSpan.Zero);

        /// <summary>The smallest possible duration.</summary>
        public static readonly IDuration MinValue = FromTimeSpan(TimeSpan.FromTicks(Ticks.PerMicroSecond));


        /// <summary>The absolute value of the movement in time between first and second.</summary>
        public static IDuration Between(ITimePoint first, ITimePoint second)
        {
            if (first.IsAfterOrSameInstantAs(second))
            {
                return FromTimeSpan(first.DateTimeValue() - second.DateTimeValue());
            }
            return FromTimeSpan(second.DateTimeValue() - first.DateTimeValue());
        }

        /// <summary>True if <paramref name="me"/> has the exact same length in time as <paramref name="other"/></summary>
        public static bool HasDurationEqualTo(this IDuration me, IDuration other)
        {
            return me.TimeSpanValue() == other.TimeSpanValue();
        }

        /// <summary>True if <paramref name="me"/> has zero length in time.</summary>
        public static bool HasZeroDuration(this IDuration me)
        {
            return me.HasDurationEqualTo(Zero);
        }

        private static IDuration FromTimeSpan(TimeSpan timeSpan)
        {
            return SimpleDuration.FromTimeSpan(timeSpan);
        }

        private class SimpleDuration : IDuration
        {
            public TimeSpan TimeSpanValue { get; private set; }

            public static IDuration FromTimeSpan(TimeSpan timeSpan)
            {
                return new SimpleDuration(timeSpan);
            }

            private SimpleDuration(TimeSpan value)
            {
                TimeSpanValue = value;
            }
        }

        #region enable non-warning access to internal use only members

#pragma warning disable 618

        private static DateTime DateTimeValue(this ITimePoint me)
        {
            return me.DateTimeValue;
        }

        private static TimeSpan TimeSpanValue(this IDuration me)
        {
            return me.TimeSpanValue;
        }

#pragma warning restore 618

        #endregion
    }
}