using Void.Time.Impl;

namespace Void.Time
{
    public static class TimeInterval
    {
        public static ITimePoint EndTime(this ITimeInterval me)
        {
            return me.TimePosition.Offset(me);
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

        public static ITimeInterval IntersectionWith(this ITimeInterval me, ITimeInterval other)
        {
            var start = TimePoint.Max(me.StartTime(), other.StartTime());
            var end = TimePoint.Min(me.EndTime(), other.EndTime());
            return NewTimeInterval(start, end);
        }

        public static bool InterSects(this ITimeInterval me, ITimeInterval other)
        {
            return !me.IntersectionWith(other).IsEmpty();
        }

        public static bool IsEmpty(this ITimeInterval me)
        {
            return me.HasZeroDuration();
        }

        #region enable non-warning access to internal use only members
        #pragma warning disable 618

        private static SimpleTimeInterval NewTimeInterval(ITimePoint startingPoint, ITimePoint endPoint)
        {
            return new SimpleTimeInterval(startingPoint, Duration.Between(startingPoint, endPoint));
        }

        #pragma warning restore 618
        #endregion
    }
}