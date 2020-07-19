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
        internal static TimeSpan IfInstrumentedMultiplyBy(this TimeSpan original, double by) =>
            Performance.IsInstrumented ? original * by : original;

        internal static int IfInstrumentedDivideBy(this int original, double by) =>
            Performance.IsInstrumented ? (int)(original / by) : original;

        internal static class Performance
        {
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

                return time.Total > 1.Milliseconds();
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
