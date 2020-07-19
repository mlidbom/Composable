using System;
using System.Globalization;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Diagnostics;
using NCrunch.Framework;

namespace Composable.Testing
{
    ///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
    static partial class TestEnv
    {
        //todo: It is rather confusing how one of these identically named methods divides by the slowdownFactor and the other multiplies. Do something about the names.
        internal static TimeSpan InstrumentationSlowdown(this TimeSpan original, double slowdownFactor) => Performance.InstrumentationSlowdown(original, slowdownFactor);
        internal static int InstrumentationSlowdown(this int original, double slowdownFactor) => Performance.InstrumentationSlowdown(original, slowdownFactor);

        internal static class Performance
        {
            internal static TimeSpan InstrumentationSlowdown(TimeSpan original, double slowdownFactor)
            {
                if(IsInstrumented)
                {
                    return ((int)(original.TotalMilliseconds * slowdownFactor)).Milliseconds();
                } else
                {
                    return original;
                }
            }

            internal static int InstrumentationSlowdown(int original, double slowdownFactor)
            {
                if(IsInstrumented)
                {
                    return (int)(original / slowdownFactor);
                } else
                {
                    return original;
                }
            }

            internal static readonly bool IsInstrumented = CheckIfInstrumented();
            static bool CheckIfInstrumented()
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if(!IsRunningUnderNCrunch) return false;

                var time = StopwatchExtensions.TimeExecution(() =>
                                                             {
                                                                 for(int i = 0; i < 100; i++)
                                                                 {
                                                                     // ReSharper disable once UnusedVariable
                                                                     int something = i;
                                                                 }
                                                             },
                                                             iterations: 500);

                return time.Total.TotalMilliseconds > 1;
            }

            public static readonly double MachineSlowdownFactor = DetectEnvironmentPerformanceAdjustment();
            public const string MachineSlowdownFactorEnvironmentVariable = "COMPOSABLE_MACHINE_SLOWNESS";
            static double DetectEnvironmentPerformanceAdjustment()
            {
                var environmentOverride = Environment.GetEnvironmentVariable(MachineSlowdownFactorEnvironmentVariable);
                if(environmentOverride != null)
                {
                    if(!Double.TryParse(environmentOverride, NumberStyles.Any, CultureInfo.InvariantCulture, out var adjustment))
                    {
                        throw new Exception($"Environment variable har invalid value: {MachineSlowdownFactorEnvironmentVariable}. It should be parsable as a double.");
                    }

                    return adjustment;
                }

                return 1.0;
            }
            public static TimeSpan? AdjustTime(TimeSpan? timespan) => timespan?.MultiplyBy(MachineSlowdownFactor);
            public static void LogTimeAdjustment()
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if(MachineSlowdownFactor != 1.0)
                {
                    Console.WriteLine($"Adjusting allowed execution time with value {MachineSlowdownFactor} from environment variable {MachineSlowdownFactorEnvironmentVariable}");
                }
            }

            static bool IsRunningUnderNCrunch
            {
                get
                {
#if NCRUNCH
    return true;
#else
                    return false;
#endif
                }
            }
        }
    }
}
