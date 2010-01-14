using System;

namespace Void.Time
{
    ///<summary>Methods on an <see cref="ITimeInterval"/></summary>
    public static class TimeInterval
    {
        /// <summary>The last <see cref="ITimePoint"/> before <paramref name="me"/> that is not part of the interval.<paramref name="me"/></summary>
        public static ITimePoint LastInstantBefore(this ITimeInterval me)
        {
            return me.FirstInstant().PreviousInstant();
        }

        /// <summary>The first <see cref="ITimePoint"/> that the <see cref="ITimeInterval"/> <see cref="Contains(ITimeInterval,ITimePoint)"/> (Given that the interval has non-zero duration, othervise it contains no points and this point is simply its location in time..)</summary>
        public static ITimePoint FirstInstant(this ITimeInterval me)
        {
            return me.TimePosition;
        }

        /// <summary>The last <see cref="ITimePoint"/> that the <see cref="ITimeInterval"/> <see cref="Contains(ITimeInterval,ITimePoint)"/> (Given that the interval has non-zero duration, othervise it contains no points and this point is simply its location in time..)</summary>
        public static ITimePoint LastInstant(this ITimeInterval me)
        {
            return me.FirstInstantAfter().PreviousInstant();
        }

        /// <summary>The first <see cref="ITimePoint"/> after the interval.</summary>
        public static ITimePoint FirstInstantAfter(this ITimeInterval me)
        {
            return me.TimePosition.Offset(me);
        }


        /// <summary>True if <see cref="LastInstantBefore"/> is before <paramref name="point"/> and <see cref="FirstInstantAfter"/> is after <paramref name="point"/></summary>
        public static bool Contains(this ITimeInterval me, ITimePoint point)
        {
            return me.LastInstantBefore().IsBefore(point) && me.FirstInstantAfter().IsAfter(point);
        }

        /// <summary>True if <paramref name="me"/> contains every <see cref="ITimePoint"/> that <paramref name="other"/> contains</summary>
        public static bool Contains(this ITimeInterval me, ITimeInterval other)
        {
            return me.Contains(other.FirstInstant()) && me.Contains(other.LastInstant());
        }

        /// <summary>The <see cref="ITimeInterval"/> that <see cref="Contains(ITimeInterval,ITimePoint)"/> all the <see cref="ITimePoint"/>s that both <paramref name="other"/> and <paramref name="me"/> <see cref="Contains(ITimeInterval,ITimePoint)"/>.</summary>
        public static ITimeInterval IntersectionWith(this ITimeInterval me, ITimeInterval other)
        {
            var start = TimePoint.Latest(me.FirstInstant(), other.FirstInstant());
            var end = TimePoint.Earliest(me.LastInstant(), other.LastInstant());
            if (start.IsAfterOrSameInstantAs(end)) //Detect non-intersecting case.
            {
                return NewTimeInterval(start, Duration.Zero);
            }
            return NewTimeInterval(start, Duration.Between(start, end));
        }

        /// <summary>True if any <see cref="ITimePoint"/> within <paramref name="other"/> is within <paramref name="me"/></summary>
        public static bool InterSects(this ITimeInterval me, ITimeInterval other)
        {
            return !me.IntersectionWith(other).HasZeroDuration();
        }

        private class SimpleTimeInterval : ITimeInterval
        {
            public ITimePoint TimePosition { get; private set; }
            private TimeSpan _timeSpanValue;
            public TimeSpan TimeSpanValue
            {
                set { _timeSpanValue = value; }
            }

            public TimeSpan AsTimeSpan()
            {
                return _timeSpanValue;
            }

            public SimpleTimeInterval(ITimePoint timeCoordinate, IDuration duration)
            {
                TimePosition = timeCoordinate;
                TimeSpanValue = duration.AsTimeSpan();
            }
        }

        private static SimpleTimeInterval NewTimeInterval(ITimePoint startingPoint, IDuration duration)
        {
            return new SimpleTimeInterval(startingPoint, duration);
        }
    }
}