using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Logging;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.ThreadingCE;
using JetBrains.Annotations;

namespace Composable.Testing.Performance
{
    //performance: Add ability to switch on strict mode such that no retries are performed. This would help us surface tests riding the edge and causing extra load during test runs.
    public static class TimeAsserter
    {
        const string DefaultTimeFormat = @"ss\.fffffff";

        static readonly TimeSpan OneMicrosecond = 1.Microseconds();
        static readonly TimeSpan OneMillisecond = 1.Microseconds();

        static string Format(TimeSpan? time)
        {
            if(time == null) return "";

            if(time >= OneMillisecond)
            {
                var defaultFormattedWith7SecondDecimalPoints = time.Value.ToStringInvariant(DefaultTimeFormat);

                var parts = defaultFormattedWith7SecondDecimalPoints.Split('.');
                var (integer, decimalPart) = (parts[0], parts[1]);

                var d1 = decimalPart.Substring(0, 3);
                var d2 = decimalPart.Substring(3, 3);
                var d3 = decimalPart.Substring(6, 1);

                return $"{integer}.{d1}_{d2}_{d3}";
            }

            if(time >= OneMicrosecond)
            {
                return $"{time.Value.TotalMicroseconds()} microseconds";
            } else
            {
                return $"{time.Value.TotalNanoseconds()} nanoseconds";
            }
        }

        const int MaxTriesLimit = 10;
        const int MaxTriesDefault = 10;

        public static async Task<StopwatchCE.TimedExecutionSummary> ExecuteAsync
            ([InstantHandle]Func<Task> action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             uint maxTries = MaxTriesDefault,
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null)
        {
            Assert.Argument.Assert(maxTries > 0);
            maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
            maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
            TestEnv.Performance.LogMachineSlownessAdjustment();

            maxTries = Math.Min(MaxTriesLimit, maxTries);
            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                StopwatchCE.TimedExecutionSummary executionSummary;
                try
                {
                    executionSummary = await StopwatchCE.TimeExecutionAsync(action: action, iterations: iterations).NoMarshalling();
                }
                finally
                {
                    tearDown?.Invoke();
                }

                try
                {
                    RunAsserts(maxAverage: maxAverage, maxTotal: maxTotal, executionSummary: executionSummary, format: Format);
                }
                catch(TimeOutException e)
                {
                    SafeConsole.WriteLine($"################################  WARNING ################################ Try: {tries} : {e.Message}");
                    if(tries >= maxTries)
                    {
                        PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);
                        throw;
                    }
                    continue;
                }
                PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);
                return executionSummary;
            }

            throw new Exception("Unreachable");
        }

        public static StopwatchCE.TimedExecutionSummary Execute
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             uint maxTries = MaxTriesDefault,
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null)
        {
            Assert.Argument.Assert(maxTries > 0);
            maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
            maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
            TestEnv.Performance.LogMachineSlownessAdjustment();

            maxTries = Math.Min(MaxTriesLimit, maxTries);
            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                StopwatchCE.TimedExecutionSummary executionSummary;
                try
                {
                    executionSummary = StopwatchCE.TimeExecution(action: action, iterations: iterations);
                }
                finally
                {
                    tearDown?.Invoke();
                }

                try
                {
                    RunAsserts(maxAverage: maxAverage, maxTotal: maxTotal, executionSummary: executionSummary, format: Format);
                }
                catch(TimeOutException e)
                {
                    SafeConsole.WriteLine($"################################  WARNING ################################ Try: {tries} : {e.Message}");
                    if(tries >= maxTries)
                    {
                        PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);
                        throw;
                    }
                    continue;
                }
                PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);
                return executionSummary;
            }

            throw new Exception("Unreachable");
        }

        public static StopwatchCE.TimedThreadedExecutionSummary ExecuteThreaded
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null,
             int maxTries = MaxTriesDefault,
            int maxDegreeOfParallelism = -1)
        {
            maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
            maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
            TestEnv.Performance.LogMachineSlownessAdjustment();

            // ReSharper disable AccessToModifiedClosure
            void PrintResults(StopwatchCE.TimedThreadedExecutionSummary executionSummary)
            {
                PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);

                SafeConsole.WriteLine($@"  
    Individual execution times    
    Average: {Format(executionSummary.IndividualExecutionTimes.Average())}
    Min:     {Format(executionSummary.IndividualExecutionTimes.Min())}
    Max:     {Format(executionSummary.IndividualExecutionTimes.Max())}
    Sum:     {Format(executionSummary.IndividualExecutionTimes.Sum())}
");
            }
            // ReSharper restore AccessToModifiedClosure

            maxTries = Math.Min(MaxTriesLimit, maxTries);
            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                StopwatchCE.TimedThreadedExecutionSummary executionSummary;
                try
                {
                    executionSummary = StopwatchCE.TimeExecutionThreaded(action: action, iterations: iterations, maxDegreeOfParallelism: maxDegreeOfParallelism);
                }
                finally
                {
                    tearDown?.Invoke();
                }

                try
                {
                    RunAsserts(maxAverage, maxTotal, executionSummary, Format);
                }
                catch(TimeOutException e)
                {
                    SafeConsole.WriteLine($"################################  WARNING ################################ Try: {tries} : {e.Message}");
                    if(tries >= maxTries)
                    {
                        PrintResults(executionSummary);
                        throw;
                    }
                    continue;
                }
                PrintResults(executionSummary);
                return executionSummary;
            }
            throw new Exception("Unreachable");
        }

        public static StopwatchCE.TimedExecutionSummary ExecuteThreadedLowOverhead
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null,
             int maxTries = MaxTriesDefault,
            int maxDegreeOfParallelism = -1)
        {
            maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
            maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
            TestEnv.Performance.LogMachineSlownessAdjustment();

            maxTries = Math.Min(MaxTriesLimit, maxTries);
            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                StopwatchCE.TimedExecutionSummary executionSummary;
                try
                {
                    executionSummary = StopwatchCE.TimeExecutionThreadedLowOverhead(action: action, iterations: iterations, maxDegreeOfParallelism: maxDegreeOfParallelism);
                }
                finally
                {
                    tearDown?.Invoke();
                }

                try
                {
                    RunAsserts(maxAverage, maxTotal, executionSummary, Format);
                }
                catch(TimeOutException e)
                {
                    SafeConsole.WriteLine($"################################  WARNING ################################ Try: {tries} : {e.Message}");
                    if(tries >= maxTries)
                    {
                        PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);
                        throw;
                    }
                    continue;
                }
                PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);
                return executionSummary;
            }
            throw new Exception("Unreachable");
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        static void RunAsserts(TimeSpan? maxAverage, TimeSpan? maxTotal, StopwatchCE.TimedExecutionSummary executionSummary, [InstantHandle]Func<TimeSpan?, string> format)
        {
            if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
            {
                string maxTotalReport = $" {Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {format(maxTotal)}";

                throw new TimeOutException($"{nameof(maxTotal)}: {format(maxTotal!.Value)} exceeded. Was: {format(executionSummary.Total)} {maxTotalReport}");
            }

            if(maxAverage.HasValue && executionSummary.Average > maxAverage.Value)
            {
                string maxAverageReport = $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {format(maxAverage)}";

                throw new TimeOutException($"{nameof(maxAverage)}: {format(maxAverage!.Value)} exceeded. Was: {format(executionSummary.Average)} {maxAverageReport}");
            }
        }

        public class TimeOutException : Exception
        {
            public TimeOutException(string message) : base(message) {}
        }

        static string Percent(TimeSpan percent, TimeSpan of) => $"{(int)((percent.TotalMilliseconds / of.TotalMilliseconds) * 100)}%";

        static void PrintSummary
            (int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, string description, [InstantHandle]Func<TimeSpan?, string> format, StopwatchCE.TimedExecutionSummary executionSummary)
        {
            string maxAverageReport = maxAverage == null
                                       ? ""
                                       : $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {format(maxAverage)}";

            string maxTotalReport = maxTotal == null
                                       ? ""
                                       : $" {Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {format(maxTotal)}";

            if(iterations > 1)
            {
                SafeConsole.WriteLine(
                    $@"Executed {iterations:### ### ###} iterations of ""{description}""
    Total:   {format(executionSummary.Total)} {maxTotalReport}
    Average: {format(executionSummary.Average)} {maxAverageReport}");
            }
            else
            {
                SafeConsole.WriteLine(
                    $@"Executed {iterations} iterations of ""{description}""
    Total:   {format(executionSummary.Total)} {maxTotalReport} ");
            }
        }
    }
}