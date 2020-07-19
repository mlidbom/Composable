using System;
using System.Globalization;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Logging;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Threading;
using JetBrains.Annotations;

namespace Composable.Testing.Performance
{
    //performance: Add ability to switch on strict mode such that no retries are performed. This would help us surface tests riding the edge and causing extra load during test runs.
    public static class TimeAsserter
    {
        const string DefaultTimeFormat = @"ss\.ffffff";
        const int MaxTriesLimit = 10;
        const int MaxTriesDefault = 4;

        public static async Task<StopwatchExtensions.TimedExecutionSummary> ExecuteAsync
            ([InstantHandle]Func<Task> action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string? timeFormat = null,
             uint maxTries = MaxTriesDefault,
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null)
        {
            Assert.Argument.Assert(maxTries > 0);
            maxAverage = TestEnv.Performance.AdjustTime(maxAverage);
            maxTotal = TestEnv.Performance.AdjustTime(maxTotal);
            TestEnv.Performance.LogTimeAdjustment();

            timeFormat ??= DefaultTimeFormat;

            string Format(TimeSpan? date) => date?.ToStringInvariant(timeFormat) ?? "";

            maxTries = Math.Min(MaxTriesLimit, maxTries);
            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                StopwatchExtensions.TimedExecutionSummary executionSummary;
                try
                {
                    executionSummary = await StopwatchExtensions.TimeExecutionAsync(action: action, iterations: iterations).NoMarshalling();
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

        public static StopwatchExtensions.TimedExecutionSummary Execute
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string? timeFormat = null,
             uint maxTries = MaxTriesDefault,
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null)
        {
            Assert.Argument.Assert(maxTries > 0);
            maxAverage = TestEnv.Performance.AdjustTime(maxAverage);
            maxTotal = TestEnv.Performance.AdjustTime(maxTotal);
            TestEnv.Performance.LogTimeAdjustment();

            timeFormat ??= DefaultTimeFormat;

            string Format(TimeSpan? date) => date?.ToStringInvariant(timeFormat) ?? "";

            maxTries = Math.Min(MaxTriesLimit, maxTries);
            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                StopwatchExtensions.TimedExecutionSummary executionSummary;
                try
                {
                    executionSummary = StopwatchExtensions.TimeExecution(action: action, iterations: iterations);
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

        public static StopwatchExtensions.TimedThreadedExecutionSummary ExecuteThreaded
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string? timeFormat = null,
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null,
             int maxTries = MaxTriesDefault,
            int maxDegreeOfParallelism = -1)
        {
            maxAverage = TestEnv.Performance.AdjustTime(maxAverage);
            maxTotal = TestEnv.Performance.AdjustTime(maxTotal);
            TestEnv.Performance.LogTimeAdjustment();

            timeFormat ??= DefaultTimeFormat;


            // ReSharper disable AccessToModifiedClosure

            string Format(TimeSpan? date) => date?.ToStringInvariant(timeFormat) ?? "";

            void PrintResults(StopwatchExtensions.TimedThreadedExecutionSummary executionSummary)
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
                StopwatchExtensions.TimedThreadedExecutionSummary executionSummary;
                try
                {
                    executionSummary = StopwatchExtensions.TimeExecutionThreaded(action: action, iterations: iterations, maxDegreeOfParallelism: maxDegreeOfParallelism);
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

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        static void RunAsserts(TimeSpan? maxAverage, TimeSpan? maxTotal, StopwatchExtensions.TimedExecutionSummary executionSummary, [InstantHandle]Func<TimeSpan?, string> format)
        {
            if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
            {
                string maxTotalReport = maxTotal == null
                                         ? ""
                                         : $" {Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {format(maxTotal)}";

                throw new TimeOutException($"{nameof(maxTotal)}: {format(maxTotal!.Value)} exceeded. Was: {format(executionSummary.Total)} {maxTotalReport}");
            }

            if(maxAverage.HasValue && executionSummary.Average > maxAverage.Value)
            {
                string maxAverageReport = maxAverage == null
                                           ? ""
                                           : $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {format(maxAverage)}";

                throw new TimeOutException($"{nameof(maxAverage)}: {format(maxAverage!.Value)} exceeded. Was: {format(executionSummary.Average)} {maxAverageReport}");
            }
        }

        public class TimeOutException : Exception
        {
            public TimeOutException(string message) : base(message) {}
        }

        static string Percent(TimeSpan percent, TimeSpan of) => $"{(int)((percent.TotalMilliseconds / of.TotalMilliseconds) * 100)}%";

        static void PrintSummary
            (int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, string description, [InstantHandle]Func<TimeSpan?, string> format, StopwatchExtensions.TimedExecutionSummary executionSummary)
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