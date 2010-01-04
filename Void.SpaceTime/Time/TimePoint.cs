using System;
using Void.Time.Impl;

namespace Void.Time
{
    /// <summary>Operations on an <see cref="ITimePoint"/></summary>
    public static class TimePoint
    {
        #region projections

        /// <summary>Returns the <see cref="ITimePoint"/> resulting from <see cref="Offset"/>ing <paramref name="me"/> by the smallest possible <see cref="ITimeMovement"/> backward in time.</summary>
        public static ITimePoint PreviousInstant(this ITimePoint me)
        {
            return me.Offset(Duration.MinValue.Negate());
        }

        /// <summary>Returns the <see cref="ITimePoint"/> resulting from <see cref="Offset"/>ing <paramref name="me"/> by the smallest possible <see cref="ITimeMovement"/> forward in time.</summary>
        public static ITimePoint NextInstant(this ITimePoint me)
        {
            return me.Offset(Duration.MinValue);
        }


        /// <summary>Returns an <see cref="ITimePoint"/> that resides at the position in time that is <paramref name="movement"/> distant from <paramref name="me"/></summary>
        public static ITimePoint Offset(this ITimePoint me, ITimeMovement movement)
        {
            return FromDateTime(me.Value() + movement.Value());
        }

        #endregion

        #region comparisons


        /// <summary>True if <paramref name="me"/> is placed before <paramref name="other"/> on the timeline.</summary>
        public static bool IsBefore(this ITimePoint me, ITimePoint other)
        {
            return me.Value() < other.Value();
        }

        /// <summary>True if <paramref name="me"/> is placed before or on the same point as <paramref name="other"/> on the timeline.</summary>
        public static bool IsBeforeOrSameInstantAs(this ITimePoint me, ITimePoint other)
        {
            return me.IsBefore(other) || me.IsSameInstantAs(other);
        }

        /// <summary>True if <paramref name="me"/> is placed after <paramref name="other"/> on the timeline.</summary>
        public static bool IsAfter(this ITimePoint me, ITimePoint other)
        {
            return me.Value() > other.Value();
        }

        /// <summary>True if <paramref name="me"/> is placed after or on the same point as <paramref name="other"/> on the timeline.</summary>
        public static bool IsAfterOrSameInstantAs(this ITimePoint me, ITimePoint other)
        {
            return me.IsAfter(other) || me.IsSameInstantAs(other);
        }

        /// <summary>Returns the latest of the two points in time</summary>
        public static T Latest<T>(T first, T second) where T : ITimePoint
        {
            return first.Value() > second.Value() ? first : second;
        }

        /// <summary>Returns the earliest of the two points in time</summary>
        public static T Earliest<T>(T first, T second) where T : ITimePoint
        {
            return first.Value() < second.Value() ? first : second;
        }

        private static bool IsSameInstantAs(this ITimePoint me, ITimePoint other)
        {
            return me.Value() == other.Value();
        }

        #endregion

        private class SimpleTimePoint : ITimePoint
        {
            public DateTime DateTimeValue { get; private set; }
            public ITimePoint TimePosition { get { return this; } }

            public SimpleTimePoint(DateTime position)
            {
                DateTimeValue = position;
            }            
        }        

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