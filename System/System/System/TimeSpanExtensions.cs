#region usings

using System;

#endregion

namespace Composable.System
{
    /// <summary>A collection of extensions to work with timespans</summary>
    public static class TimeSpanExtensions
    {
        /// <summary>Returns a TimeSpan <paramref name="me"/> milliseconds long.</summary>
        public static TimeSpan Milliseconds(this int me)
        {
            return new TimeSpan(0, 0, 0, 0, me);
        }

        /// <summary>Returns a TimeSpan <paramref name="me"/> seconds long.</summary>
        public static TimeSpan Seconds(this int me)
        {
            return TimeSpan.FromSeconds(me);
        }

        /// <summary>Returns a TimeSpan <paramref name="me"/> seconds long.</summary>
        public static TimeSpan Seconds(this double me)
        {
            return TimeSpan.FromSeconds(me);
        }


        /// <summary>Returns a TimeSpan <paramref name="me"/> minutes long.</summary>
        public static TimeSpan Minutes(this int me)
        {
            return TimeSpan.FromMinutes(me);
        }

        /// <summary>Returns a TimeSpan <paramref name="me"/> minutes long.</summary>
        public static TimeSpan Minutes(this double me)
        {
            return TimeSpan.FromMinutes(me);
        }

        /// <summary>Returns a TimeSpan <paramref name="me"/> hours long.</summary>
        public static TimeSpan Hours(this int me)
        {
            return TimeSpan.FromHours(me);
        }

        /// <summary>Returns a TimeSpan <paramref name="me"/> hours long.</summary>
        public static TimeSpan Hours(this double me)
        {
            return TimeSpan.FromHours(me);
        }


        /// <summary>Returns a TimeSpan <paramref name="me"/> days long.</summary>
        public static TimeSpan Days(this int me)
        {
            return TimeSpan.FromDays(me);
        }

        /// <summary>Returns a TimeSpan <paramref name="me"/> days long.</summary>
        public static TimeSpan Days(this double me)
        {
            return TimeSpan.FromDays(me);
        }


    }
}