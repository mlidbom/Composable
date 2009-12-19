using Void.Time.Impl;

namespace Void.Time
{
    public static class TimePoint
    {
        public static ITimePoint Offset(this ITimePoint me, ITimeMovement movement)
        {
            return new SimpleTimePoint(me.DateTimeValue +  movement.AsTimeSpan());
        }
    }
}