#region usings

using System;

#endregion

namespace Composable.SpaceTime.Time
{
    /// <summary>Operations on an <see cref="ITimeMovement"/></summary>
    public static class TimeMovement
    {
        /// <summary>Returns an <see cref="ITimeMovement"/> with the same magnitude, but opposite direction.</summary>
        public static ITimeMovement Negate(this ITimeMovement me)
        {
            return new FieldBackedTimeMovement(me.AsTimeSpan().Negate());
        }

        private class FieldBackedTimeMovement : ITimeMovement
        {
            public FieldBackedTimeMovement(TimeSpan value)
            {
                TimeSpanValue = value;
                Action<object> printObject = toPrint => Console.WriteLine(toPrint);
            }

            private TimeSpan _timeSpanValue;
            public TimeSpan TimeSpanValue { set { _timeSpanValue = value; } }

            public TimeSpan AsTimeSpan()
            {
                return _timeSpanValue;
            }
        }
    }
}