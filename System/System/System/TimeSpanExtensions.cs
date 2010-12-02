#region usings

using System;

#endregion

namespace Composable.System
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Milliseconds(this int me)
        {
            return new TimeSpan(0, 0, 0, 0, me);
        }

        public static TimeSpan Seconds(this int me)
        {
            return TimeSpan.FromSeconds(me);
        }

        public static TimeSpan Seconds(this double me)
        {
            return TimeSpan.FromSeconds(me);
        }


        public static TimeSpan Minutes(this int me)
        {
            return TimeSpan.FromMinutes(me);
        }

        public static TimeSpan Minutes(this double me)
        {
            return TimeSpan.FromMinutes(me);
        }

        public static TimeSpan Hours(this int me)
        {
            return TimeSpan.FromHours(me);
        }

        public static TimeSpan Hours(this double me)
        {
            return TimeSpan.FromHours(me);
        }

        public static TimeSpan Days(this int me)
        {
            return TimeSpan.FromDays(me);
        }

        public static TimeSpan Days(this double me)
        {
            return TimeSpan.FromDays(me);
        }


    }
}