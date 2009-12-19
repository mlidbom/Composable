using System;
using Void.Time.Impl;

namespace Void.Time
{
    public static class TimePoint
    {
        public static ITimePoint FromDateTime(DateTime time)
        {
            return new SimpleTimePoint(time);
        }

        public static bool IsBefore(this ITimePoint me, ITimePoint other)
        {
            return me.DateTimeValue < other.DateTimeValue;
        }

        public static bool IsBeforeOrEqualTo(this ITimePoint me, ITimePoint other)
        {
            return me.IsBefore(other) || me.DateTimeValue == other.DateTimeValue;
        }

        public static bool IsAfter(this ITimePoint me, ITimePoint other)
        {
            return me.DateTimeValue > other.DateTimeValue;
        }

        public static ITimePoint Offset(this ITimePoint me, ITimeMovement movement)
        {
            return FromDateTime(me.DateTimeValue + movement.AsTimeSpan());
        }

        public static T Max<T>(T first, T second) where T : ITimePoint
        {
            return first.DateTimeValue > second.DateTimeValue ? first : second;
        }

        public static T Min<T>(T first, T second) where T : ITimePoint
        {
            return first.DateTimeValue < second.DateTimeValue ? first : second;
        }
    }
}