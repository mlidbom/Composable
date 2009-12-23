using System;

namespace Void.Time
{
    ///<summary>Methods on an <see cref="ITimeInterval"/></summary>
    public static class TimeInterval
    {
        /// <summary>The last <see cref="ITimePoint"/> before <paramref name="me"/> that is not contained within <paramref name="me"/></summary>
        public static ITimePoint LastInstantBefore(this ITimeInterval me)
        {
            return me.FirstInstant().PreviousInstant();
        }

        /// <summary>The first <see cref="ITimePoint"/> that the <see cref="ITimeInterval"/> <see cref="Contains(ITimeInterval,ITimePoint)"/> (Given that the interval has non-zero duration, othervise it contains no points..)</summary>
        public static ITimePoint FirstInstant(this ITimeInterval me)
        {
            return me.TimePosition;
        }

        /// <summary>The last <see cref="ITimePoint"/> that the <see cref="ITimeInterval"/> <see cref="Contains(ITimeInterval,ITimePoint)"/> (Given that the interval has non-zero duration, othervise it contains no points..)</summary>
        public static ITimePoint LastInstant(this ITimeInterval me)
        {
            return me.FirstInstantAfter().PreviousInstant();
        }

        /// <summary>The first instant after <paramref name="me"/></summary>
        public static ITimePoint FirstInstantAfter(this ITimeInterval me)
        {
            return me.TimePosition.Offset(me);
        }


        /// <summary>True if <see cref="FirstInstant"/> is concurrent with or earlier than <paramref name="point"/> and <see cref="LastInstant"/> is after <paramref name="point"/></summary>
        public static bool Contains(this ITimeInterval me, ITimePoint point)
        {
            return me.LastInstantBefore().IsBefore(point) && me.FirstInstantAfter().IsAfter(point);
        }

        /// <summary>True if every <see cref="ITimePoint"/> this is within <paramref name="other"/> is within <paramref name="me"/></summary>
        public static bool Contains(this ITimeInterval me, ITimeInterval other)
        {
            return me.Contains(other.FirstInstant()) && me.Contains(other.LastInstant());
        }

        /// <summary>The <see cref="ITimeInterval"/> that contains all the <see cref="ITimePoint"/>s that are within both <paramref name="other"/> and <paramref name="me"/></summary>
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

        [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
        private class SimpleTimeInterval : ITimeInterval
        {
            public ITimePoint TimePosition { get; private set; }
            public TimeSpan TimeSpanValue { get; private set; }

            public SimpleTimeInterval(ITimePoint timeCoordinate, IDuration duration)
            {
                TimePosition = timeCoordinate;
                TimeSpanValue = duration.TimeSpanValue;
            }
        }

        #region enable non-warning access to internal use only members
#pragma warning disable 618

        private static SimpleTimeInterval NewTimeInterval(ITimePoint startingPoint, IDuration duration)
        {
            return new SimpleTimeInterval(startingPoint, duration);
        }

#pragma warning restore 618
        #endregion
    }
}