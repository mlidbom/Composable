using System;

namespace Void.Time
{
    ///<summary>Methods on an <see cref="ITimeInterval"/></summary>
    public static class TimeInterval
    {
        /// <summary>The first instant after <paramref name="me"/></summary>
        public static ITimePoint FirstInstantAfter(this ITimeInterval me)
        {
            return me.TimePosition.Offset(me);
        }

        /// <summary>The last instant within the inteval.</summary>
        public static ITimePoint LastInstantWithin(this ITimeInterval me)
        {
            return me.FirstInstantAfter().PreviousInstant();
        }

        /// <summary>The earliest <see cref="ITimePoint"/> that is within the <see cref="ITimeInterval"/></summary>
        public static ITimePoint FirstInstantWithin(this ITimeInterval me)
        {
            return me.TimePosition;
        }

        /// <summary>The last <see cref="ITimePoint"/> before <paramref name="me"/> that is not contained within <paramref name="me"/></summary>
        public static ITimePoint LastInstantBefore(this ITimeInterval me)
        {
            return me.FirstInstantWithin().PreviousInstant();
        }

        /// <summary>True if <paramref name="point"/> is within <paramref name="me"/></summary>
        public static bool Contains(this ITimeInterval me, ITimePoint point)
        {
            return me.LastInstantBefore().IsBefore(point) && me.FirstInstantAfter().IsAfter(point);
        }

        /// <summary>True if every <see cref="ITimePoint"/> this is within <paramref name="other"/> is within <paramref name="me"/></summary>
        public static bool Contains(this ITimeInterval me, ITimeInterval other)
        {
            return me.Contains(other.FirstInstantWithin()) && me.Contains(other.LastInstantWithin());
        }

        /// <summary>The <see cref="ITimeInterval"/> that contains all the <see cref="ITimePoint"/>s that are within both <paramref name="other"/> and <paramref name="me"/></summary>
        public static ITimeInterval IntersectionWith(this ITimeInterval me, ITimeInterval other)
        {
            var start = TimePoint.Latest(me.FirstInstantWithin(), other.FirstInstantWithin());
            var end = TimePoint.Earliest(me.LastInstantWithin(), other.LastInstantWithin());
            if (start.IsAfterOrSameInstantAs(end)) //Detect empty case.
            {
                return NewTimeInterval(start, Duration.Zero);
                ;
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