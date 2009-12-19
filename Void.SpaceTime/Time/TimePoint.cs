using System;
using Void.Time.Impl;

namespace Void.Time
{
    public static class TimePoint
    {
        public static ITimePoint Offset(this ITimePoint me, ITimeMovement movement)
        {
            return FromDateTime(me.Value() + movement.Value());
        }

        #region comparisons

        private static bool TimeEquals(this ITimePoint me, ITimePoint other)
        {
            return me.Value() == other.Value();
        }

        public static bool IsBefore(this ITimePoint me, ITimePoint other)
        {
            return me.Value() < other.Value();
        }

        public static bool IsBeforeOrEqualTo(this ITimePoint me, ITimePoint other)
        {
            return me.IsBefore(other) || me.TimeEquals(other);
        }

        public static bool IsAfter(this ITimePoint me, ITimePoint other)
        {
            return me.Value() > other.Value();
        }

        public static bool IsAfterOrEqual(this ITimePoint me, ITimePoint other)
        {
            return me.IsAfter(other) || me.TimeEquals(other);
        }

        public static T Max<T>(T first, T second) where T : ITimePoint
        {
            return first.Value() > second.Value() ? first : second;
        }

        public static T Min<T>(T first, T second) where T : ITimePoint
        {
            return first.Value() < second.Value() ? first : second;
        }

        #endregion

        #region enable non-warning access to internal use only members
#pragma warning disable 618
        private static TimeSpan Value(this ITimeMovement movement)
        {
            return movement.TimeSpanValue;
        }

        private static ITimePoint FromDateTime(DateTime time)
        {
            return new SimpleTimePoint(time);
        }

        private static DateTime Value(this ITimePoint me)
        {
            return me.DateTimeValue;
        }

#pragma warning restore 618
        #endregion
    }
}