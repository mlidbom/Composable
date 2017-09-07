using System;

// ReSharper disable UnusedMember.Global todo: write the ridiculously simple tests.
namespace Composable.Testing.System
{
    /// <summary>A collection of extensions to work with timespans</summary>
    public static class TimeSpanExtensions
    {
        /// <summary>Returns a TimeSpan <paramref name="me"/> milliseconds long.</summary>
        public static TimeSpan Milliseconds(this int me) => TimeSpan.FromMilliseconds(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> milliseconds long.</summary>
        public static TimeSpan Milliseconds(this long me) => TimeSpan.FromMilliseconds(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> milliseconds long.</summary>
        public static TimeSpan Milliseconds(this double me) => TimeSpan.FromMilliseconds(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> seconds long.</summary>
        public static TimeSpan Seconds(this int me) => TimeSpan.FromSeconds(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> seconds long.</summary>
        public static TimeSpan Seconds(this long me) => TimeSpan.FromSeconds(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> seconds long.</summary>
        public static TimeSpan Seconds(this double me) => TimeSpan.FromSeconds(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> minutes long.</summary>
        public static TimeSpan Minutes(this int me) => TimeSpan.FromMinutes(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> minutes long.</summary>
        public static TimeSpan Minutes(this long me) => TimeSpan.FromMinutes(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> minutes long.</summary>
        public static TimeSpan Minutes(this double me) => TimeSpan.FromMinutes(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> hours long.</summary>
        public static TimeSpan Hours(this int me) => TimeSpan.FromHours(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> hours long.</summary>
        public static TimeSpan Hours(this long me) => TimeSpan.FromHours(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> hours long.</summary>
        public static TimeSpan Hours(this double me) => TimeSpan.FromHours(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> days long.</summary>
        public static TimeSpan Days(this int me) => TimeSpan.FromDays(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> days long.</summary>
        public static TimeSpan Days(this long me) => TimeSpan.FromDays(me);

        /// <summary>Returns a TimeSpan <paramref name="me"/> days long.</summary>
        public static TimeSpan Days(this double me) => TimeSpan.FromDays(me);
    }
}