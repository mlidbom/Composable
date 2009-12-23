using System;

namespace Void.Time
{
    /// <summary>Operations on an <see cref="ITimeMovement"/></summary>
    public static class TimeMoveMent
    {
        /// <summary>Returns an <see cref="ITimeMovement"/> with the same magnitude, but opposite direction.</summary>
        public static ITimeMovement Negate(this ITimeMovement me)
        {
            TimeSpan movement = me.TimeSpanValue().Negate();
            return new FieldBackedTimeMovement(movement);
        }

        #region enable non-warning access to internal use only members
#pragma warning disable 618

        private static TimeSpan TimeSpanValue(this ITimeMovement me)
        {
            return me.TimeSpanValue;
        }

#pragma warning restore 618
        #endregion

        private class FieldBackedTimeMovement : ITimeMovement
        {
            public FieldBackedTimeMovement(TimeSpan value)
            {
                TimeSpanValue = value;
            }

            public TimeSpan TimeSpanValue { get; private set; }
        }
    }
}