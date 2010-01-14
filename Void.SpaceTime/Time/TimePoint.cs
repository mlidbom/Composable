using System;

namespace Void.Time
{
    /// <summary>Operations on an <see cref="ITimePoint"/></summary>
    public static class TimePoint
    {
        #region projections

        /// <summary>Returns the <see cref="ITimePoint"/> resulting from offsetting <paramref name="me"/> by the smallest possible <see cref="ITimeMovement"/> backward in time.</summary>
        public static ITimePoint PreviousInstant(this ITimePoint me)
        {
            return me.Offset(Duration.MinValue.Negate());
        }

        /// <summary>Returns the <see cref="ITimePoint"/> resulting from offsetting <paramref name="me"/> by the smallest possible <see cref="ITimeMovement"/> forward in time.</summary>
        public static ITimePoint NextInstant(this ITimePoint me)
        {
            return me.Offset(Duration.MinValue);
        }

        #endregion

        #region comparisons

        /// <summary>True if <paramref name="me"/> is placed before <paramref name="other"/> on the timeline.</summary>
        public static bool IsBefore(this ITimePoint me, ITimePoint other)
        {
            return me.AsDateTime() < other.AsDateTime();
        }

        /// <summary>True if <paramref name="me"/> is placed before or on the same point as <paramref name="other"/> on the timeline.</summary>
        public static bool IsBeforeOrSameInstantAs(this ITimePoint me, ITimePoint other)
        {
            return !me.IsAfter(other);
        }

        /// <summary>True if <paramref name="me"/> is placed after <paramref name="other"/> on the timeline.</summary>
        public static bool IsAfter(this ITimePoint me, ITimePoint other)
        {
            return me.AsDateTime() > other.AsDateTime();
        }

        /// <summary>True if <paramref name="me"/> is placed after or on the same point as <paramref name="other"/> on the timeline.</summary>
        public static bool IsAfterOrSameInstantAs(this ITimePoint me, ITimePoint other)
        {
            return !me.IsBefore(other);
        }

        /// <summary>Returns the latest of the two points in time</summary>
        public static T Latest<T>(T first, T second) where T : ITimePoint
        {
            return first.AsDateTime() > second.AsDateTime() ? first : second;
        }

        /// <summary>Returns the earliest of the two points in time</summary>
        public static T Earliest<T>(T first, T second) where T : ITimePoint
        {
            return first.AsDateTime() < second.AsDateTime() ? first : second;
        }

        private static bool IsSameInstantAs(this ITimePoint me, ITimePoint other)
        {
            return me.AsDateTime() == other.AsDateTime();
        }

        #endregion

        /// <summary>Returns a <see cref="ITimePoint"/> representing the given DateTime</summary>
        public static ITimePoint FromDateTime(DateTime time)
        {
            return new SimpleTimePoint(time);
        }

        private class SimpleTimePoint : ITimePoint
        {
            private readonly DateTime _dateTimeValue;

            public DateTime AsDateTime()
            {
                return _dateTimeValue;
            }

            public ITimePoint ProjectAt(ITimePoint targetTime)
            {
                return new SimpleTimePoint(targetTime.AsDateTime());
            }

            public ITimePoint TimePosition
            {
                get { return this; }
            }

            public SimpleTimePoint(DateTime position)
            {
                _dateTimeValue = position;
            }
        }
    }
}