using System;
using System.Globalization;
using Composable.Contracts;
using Composable.Logging;
using Composable.System;
using Composable.System.Diagnostics;
using JetBrains.Annotations;

namespace Composable.Testing.Performance
{
    public static class TimeAsserter
    {
        const string DefaultTimeFormat = "ss\\.fff";
        const string ShortTimeFormat = "ss\\.ffffff";

        static readonly double MachineSlowdownFactor = DetectEnvironmentPerformanceAdjustment();

        static double DetectEnvironmentPerformanceAdjustment()
        {
            const string machineSlowdownfactor = "COMPOSABLE_MACHINE_SLOWNESS";
            var enviromentOverride = Environment.GetEnvironmentVariable(machineSlowdownfactor);
            if(enviromentOverride != null)
            {
                if(!double.TryParse(enviromentOverride, NumberStyles.Any, CultureInfo.InvariantCulture, out var adjustment))
                {
                    throw new Exception($"Environment variable har invalid value: {machineSlowdownfactor}. It should be parsable as a double.");
                }

                return adjustment;
            }

            return 1.0;
        }

        static TimeSpan? AdjustTime(TimeSpan? timespan) => timespan?.MultiplyBy(MachineSlowdownFactor);

        public static StopwatchExtensions.TimedExecutionSummary Execute
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string? timeFormat = null,
             uint maxTries = 10,
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null)
        {
            Assert.Argument.Assert(maxTries > 0);
            maxAverage = AdjustTime(maxAverage);
            maxTotal = AdjustTime(maxTotal);

            if(timeFormat == null)
            {
                if(maxTotal.HasValue && maxTotal.Value.TotalMilliseconds < 10 || maxAverage.HasValue && maxAverage.Value.TotalMilliseconds < 10)
                {
                    timeFormat = ShortTimeFormat;
                } else
                {
                    timeFormat = DefaultTimeFormat;
                }
            }

            string Format(TimeSpan? date) => date?.ToString(timeFormat) ?? "";

            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                var executionSummary = StopwatchExtensions.TimeExecution(action: action, iterations: iterations);
                tearDown?.Invoke();
                try
                {
                    RunAsserts(maxAverage: maxAverage, maxTotal: maxTotal, executionSummary: executionSummary, format: Format);
                }
                catch(TimeOutException e)
                {
                    SafeConsole.WriteLine($"Try: {tries} {e.Message}");
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
             bool timeIndividualExecutions = false,
             string description = "",
             string? timeFormat = null,
             [InstantHandle]Action? setup = null,
             [InstantHandle]Action? tearDown = null,
             int maxTries = 10,
            int maxDegreeOfParallelism = -1)
        {
            maxAverage = AdjustTime(maxAverage);
            maxTotal = AdjustTime(maxTotal);

            if(timeFormat == null)
            {
                if(maxTotal.HasValue && maxTotal.Value.TotalMilliseconds < 10 || maxAverage.HasValue && maxAverage.Value.TotalMilliseconds < 10)
                {
                    timeFormat = ShortTimeFormat;
                } else
                {
                    timeFormat = DefaultTimeFormat;
                }
            }


            // ReSharper disable AccessToModifiedClosure

            string Format(TimeSpan? date) => date?.ToString(timeFormat) ?? "";

            void PrintResults(StopwatchExtensions.TimedThreadedExecutionSummary executionSummary)
            {
                PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);

                if (timeIndividualExecutions)
                {
                    SafeConsole.WriteLine($@"  
    Individual execution times    
    Average: {Format(executionSummary.IndividualExecutionTimes.Average())}
    Min:     {Format(executionSummary.IndividualExecutionTimes.Min())}
    Max:     {Format(executionSummary.IndividualExecutionTimes.Max())}
    Sum:     {Format(executionSummary.IndividualExecutionTimes.Sum())}");
                }
            }
            // ReSharper restore AccessToModifiedClosure


            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                var executionSummary  = StopwatchExtensions.TimeExecutionThreaded(action: action, iterations: iterations, timeIndividualExecutions: timeIndividualExecutions, maxDegreeOfParallelism: maxDegreeOfParallelism);
                tearDown?.Invoke();
                try
                {
                    RunAsserts(maxAverage, maxTotal, executionSummary, Format);
                }
                catch(TimeOutException e)
                {
                    SafeConsole.WriteLine($"Try: {tries} {e.GetType() .FullName}: {e.Message}");
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

        static void RunAsserts(TimeSpan? maxAverage, TimeSpan? maxTotal, StopwatchExtensions.TimedExecutionSummary executionSummary, [InstantHandle]Func<TimeSpan?, string> format)
        {
            if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
            {
                throw new TimeOutException($"{nameof(maxTotal)}: {format(maxTotal.Value)} exceeded. Was: {format(executionSummary.Total)}");
            }

            if(maxAverage.HasValue && executionSummary.Average > maxAverage.Value)
            {
                throw new TimeOutException($"{nameof(maxAverage)} exceeded");
            }
        }

        class TimeOutException : Exception
        {
            public TimeOutException(string message) : base(message) {}
        }

        static void PrintSummary
            (int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, string description, [InstantHandle]Func<TimeSpan?, string> format, StopwatchExtensions.TimedExecutionSummary executionSummary)
        {
            if(iterations > 1)
            {
                SafeConsole.WriteLine(
                    $@"Executed {iterations:### ### ###} iterations of ""{description}""
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)} 
    Average: {format
                        (executionSummary.Average)} Limit: {format(maxAverage)}");
            }
            else
            {
                SafeConsole.WriteLine(
                    $@"Executed {iterations} iterations of ""{description}""
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)}");
            }
        }
    }
}