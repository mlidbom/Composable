using System;
using System.Globalization;

// ReSharper disable UnusedMember.Global
namespace Composable.SystemCE
{
    /// <summary>A collection of extensions to work with timespans</summary>
    static partial class TimeSpanCE
    {
        const double TicksPerMicroSecond = TimeSpan.TicksPerMillisecond / 1000.0;
        const double TicksPerNanosecond = TicksPerMicroSecond / 1000.0;

        public static TimeSpan Ticks(this double ticks) => TimeSpan.FromTicks((long)Math.Round(ticks));
        public static TimeSpan Ticks(this long ticks) => TimeSpan.FromTicks(ticks);

        public static TimeSpan Nanoseconds(this int nanoseconds) => (nanoseconds * TicksPerNanosecond).Ticks();

        public static TimeSpan Microseconds(this int microseconds) => (microseconds * TicksPerMicroSecond).Ticks();

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

        public static TimeSpan MultiplyBy(this TimeSpan @this, double times) => TimeSpan.FromTicks((long)(@this.Ticks * times));

        public static TimeSpan DivideBy(this TimeSpan @this, double divideBy) => TimeSpan.FromTicks((long)(@this.Ticks / divideBy));

        internal static string ToStringInvariant(this TimeSpan @this, string format) => @this.ToString(format, CultureInfo.InvariantCulture);
    }
}