using System;
using Void.Time.Impl;

namespace Void.Time
{
    public static class TimeInterval
    {
        public static ITimePoint EndTime(this ITimeInterval me)
        {
            return new SimpleTimePoint(me.TimePosition.DateTimeValue + me.Duration);
        }

        public static ITimePoint StartTime(this ITimeInterval me)
        {
            return me.TimePosition;
        }

        public static bool Contains(this ITimeInterval me, ITimePoint point)
        {
            return me.StartTime().IsBeforeOrEqualTo(point) && me.EndTime().IsAfter(point);
        }

        public static bool Contains(this ITimeInterval me, ITimeInterval other)
        {
            return me.Contains(other.StartTime()) && me.Contains(other.EndTime());
        }

        public static bool InterSects(this ITimeInterval me, ITimeInterval other)
        {
            return TimePoint.Max(me.StartTime(), other.StartTime()).IsBefore(TimePoint.Min(me.EndTime(), other.EndTime()));
        }
    }
}