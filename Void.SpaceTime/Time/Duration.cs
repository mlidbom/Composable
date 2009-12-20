using System;
using Void.Time;

namespace Void.Time
{
    public static class Duration
    {
        public static readonly IDuration Zero = new SimpleDuration(TimeSpan.Zero);

        public static IDuration Between(ITimePoint first, ITimePoint second)
        {
            if(first.IsAfterOrEqual(second))
            {
                return Zero;
            }
            return new SimpleDuration(second.DateTimeValue() - first.DateTimeValue());
        }

        public static bool DurationEquals(this IDuration me, IDuration other)
        {
            return me.TimeSpanValue() == other.TimeSpanValue();
        }

        public static bool HasZeroDuration(this IDuration me)
        {
            return me.DurationEquals(Zero);
        }

        private class SimpleDuration : IDuration
        {
            public SimpleDuration(TimeSpan value)
            {
                TimeSpanValue = value;
            }
            public TimeSpan TimeSpanValue { get; private set;}
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