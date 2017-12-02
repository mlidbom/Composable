using System;


// ReSharper disable UnusedMember.Global todo: write the ridiculously simple tests.
namespace Composable.System
{
    /// <summary>A collection of extensions to work with timespans</summary>
    static class TimeSpanExtensions
    {
        /// <summary>Returns a TimeSpan <paramref name="this"/> milliseconds long.</summary>
        public static TimeSpan Milliseconds(this int @this) => TimeSpan.FromMilliseconds(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> milliseconds long.</summary>
        public static TimeSpan Milliseconds(this long @this) => TimeSpan.FromMilliseconds(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> milliseconds long.</summary>
        public static TimeSpan Milliseconds(this double @this) => TimeSpan.FromMilliseconds(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> seconds long.</summary>
        public static TimeSpan Seconds(this int @this) => TimeSpan.FromSeconds(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> seconds long.</summary>
        public static TimeSpan Seconds(this long @this) => TimeSpan.FromSeconds(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> seconds long.</summary>
        public static TimeSpan Seconds(this double @this) => TimeSpan.FromSeconds(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> minutes long.</summary>
        public static TimeSpan Minutes(this int @this) => TimeSpan.FromMinutes(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> minutes long.</summary>
        public static TimeSpan Minutes(this long @this) => TimeSpan.FromMinutes(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> minutes long.</summary>
        public static TimeSpan Minutes(this double @this) => TimeSpan.FromMinutes(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> hours long.</summary>
        public static TimeSpan Hours(this int @this) => TimeSpan.FromHours(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> hours long.</summary>
        public static TimeSpan Hours(this long @this) => TimeSpan.FromHours(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> hours long.</summary>
        public static TimeSpan Hours(this double @this) => TimeSpan.FromHours(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> days long.</summary>
        public static TimeSpan Days(this int @this) => TimeSpan.FromDays(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> days long.</summary>
        public static TimeSpan Days(this long @this) => TimeSpan.FromDays(@this);

        /// <summary>Returns a TimeSpan <paramref name="this"/> days long.</summary>
        public static TimeSpan Days(this double @this) => TimeSpan.FromDays(@this);
    }
}