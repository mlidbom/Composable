using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;

namespace Composable.Testing
{
    ///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
    static partial class TestEnv
    {
        internal static TimeSpan EnvMultiply(this TimeSpan original, double instrumented = 1.0, double unoptimized = 1.0) =>
            original * EnvFactor(instrumented: instrumented, unoptimized: unoptimized);

        internal static int EnvMultiply(this int original, double instrumented = 1.0, double unoptimized = 1.0) =>
            (int)(original * EnvFactor(instrumented: instrumented, unoptimized: unoptimized));

        internal static int EnvDivide(this int original, double instrumented = 1.0, double unoptimized = 1.0) =>
            (int)(original / EnvFactor(instrumented: instrumented, unoptimized: unoptimized));

        static double EnvFactor(double instrumented = 1.0, double unoptimized = 1.0)
        {
            if(Performance.IsInstrumented) return instrumented;
            if(Performance.AreOptimizationsDisabled) return unoptimized;
            return 1.0;
        }

        internal static class Performance
        {
            internal static readonly bool AreOptimizationsDisabled = ((DebuggableAttribute)typeof(TestEnv).Assembly.GetCustomAttribute(typeof(DebuggableAttribute))!).IsJITOptimizerDisabled;

            internal static readonly bool IsInstrumented = CheckIfInstrumented();
            static bool CheckIfInstrumented()
            {
                var time = StopwatchCE.TimeExecution(action: () =>
                                                     {
                                                         for(var i = 0; i < 100; i++)
                                                         {
                                                             // ReSharper disable once UnusedVariable
                                                             var something = i;
                                                         }
                                                     },
                                                     iterations: 500);

                if(time.Total > 1.Milliseconds())
                    return true;
                else
                    return false;
            }

            static readonly double MachineSlowness = DetectEnvironmentPerformanceAdjustment();
            const string MachineSlownessEnvironmentVariable = "COMPOSABLE_MACHINE_SLOWNESS";
            static double DetectEnvironmentPerformanceAdjustment()
            {
                var environmentOverride = Environment.GetEnvironmentVariable(MachineSlownessEnvironmentVariable);
                if(environmentOverride != null)
                {
                    if(!double.TryParse(environmentOverride, NumberStyles.Any, CultureInfo.InvariantCulture, out var adjustment)) throw new Exception($"Environment variable har invalid value: {MachineSlownessEnvironmentVariable}. It should be parsable as a double.");

                    return adjustment;
                }

                return 1.0;
            }

            public static TimeSpan? AdjustForMachineSlowness(TimeSpan? timespan) => timespan?.MultiplyBy(MachineSlowness);
            public static void LogMachineSlownessAdjustment()
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if(MachineSlowness != 1.0) Console.WriteLine($"Adjusting allowed execution time with value {MachineSlowness} from environment variable {MachineSlownessEnvironmentVariable}");
            }
        }
    }
}
