using System;
using System.Globalization;

// ReSharper disable UnusedMember.Global
namespace Composable.SystemCE
{
    /// <summary>A collection of extensions to work with timespans</summary>
    static partial class TimeSpanCE
    {
        static readonly TimeSpan OneMicrosecond = 1.Microseconds();
        static readonly TimeSpan OneMillisecond = 1.Microseconds();

        const long TicksPerMillisecond = TimeSpan.TicksPerMillisecond; //10000
        const long TicksPerMicroSecond = TicksPerMillisecond / 1000;   //10
        const double MicrosecondsPerTick = 1.0 / TicksPerMicroSecond;  //0.1

        const double TicksPerNanosecond = TicksPerMicroSecond / 1000.0; //0.01
        const double NanosecondsPerTick = 1.0 / TicksPerNanosecond;     //100.0

        public static TimeSpan Ticks(this double ticks) => TimeSpan.FromTicks((long)Math.Round(ticks));
        public static TimeSpan Ticks(this long ticks) => TimeSpan.FromTicks(ticks);

        public static double TotalNanoseconds(this TimeSpan @this) => @this.Ticks * NanosecondsPerTick;

        public static TimeSpan Nanoseconds(this double nanoseconds)
        {
            const double minimumNanosecondsForReasonableConversionAccuracy = 500.0;
            if(nanoseconds < minimumNanosecondsForReasonableConversionAccuracy)
            {
                var accuracyLossPercentage = (int)((1.0 - (minimumNanosecondsForReasonableConversionAccuracy - 50) / minimumNanosecondsForReasonableConversionAccuracy) * 100.0);

                throw new ArgumentException($"{nameof(nanoseconds)} parameter must be no less than {minimumNanosecondsForReasonableConversionAccuracy} or accuracy loss might exceed {accuracyLossPercentage}% due to the accuracy of TimeSpan.");
            }

            return (nanoseconds * TicksPerNanosecond).Ticks();
        }

        public static TimeSpan Nanoseconds(this long nanoseconds) => ((double)nanoseconds).Nanoseconds();

        public static TimeSpan Nanoseconds(this int nanoseconds) => ((double)nanoseconds).Nanoseconds();

        public static double TotalMicroseconds(this TimeSpan self) => self.Ticks * MicrosecondsPerTick;

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

        internal static string FormatReadable(this TimeSpan? time) => time == null ? "" : time.Value.FormatReadable();

        internal static string FormatReadable(this TimeSpan time)
        {
            if(time >= OneMillisecond)
            {
                var defaultFormattedWith7SecondDecimalPoints = time.ToStringInvariant(@"ss\.fffffff");

                var parts = defaultFormattedWith7SecondDecimalPoints.Split('.');
                var (integer, decimalPart) = (parts[0], parts[1]);

                var d1 = decimalPart.Substring(0, 3);
                var d2 = decimalPart.Substring(3, 3);
                var d3 = decimalPart.Substring(6, 1);

                return $"{integer}.{d1}_{d2}_{d3}";
            }

            if(time >= OneMicrosecond)
            {
                return $"{time.TotalMicroseconds()} microseconds";
            } else
            {
                return $"{time.TotalNanoseconds()} nanoseconds";
            }
        }
    }
}
