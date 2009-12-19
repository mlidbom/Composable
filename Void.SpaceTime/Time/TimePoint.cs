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

        public static ITimePoint Offset(this ITimePoint me, ITimeMovement movement)
        {
            return FromDateTime(me.DateTimeValue + movement.AsTimeSpan());
        }
    }
}