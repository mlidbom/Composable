using System;
using System.Diagnostics;
using System.Threading;
using Composable.Logging;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Threading;
using JetBrains.Annotations;

namespace Composable.Testing
{
    static class TimeAsserter
    {
        static readonly ILogger Log = Logger.For(typeof(TimeAsserter));
        const string DefaultTimeFormat = "ss\\.fff";

        static readonly Lazy<PerformanceCounter> LazyTotalCpu = new Lazy<PerformanceCounter>(() => new PerformanceCounter("Processor", "% Processor Time", "_Total"));

        static readonly MachineWideSingleThreaded MachineWideSingleThreaded = MachineWideSingleThreaded.For(typeof(TimeAsserter));

        static PerformanceCounter TotalCpu => LazyTotalCpu.Value;
        static void WaitUntilCpuLoadIsBelowPercent(int percent)
        {
            const int waitMilliseconds = 20;
            // ReSharper disable once UnusedVariable this makes profiling information sane.
            var separatedForPerformanceVisibility = TotalCpu;
            InternalWait(percent, waitMilliseconds);
        }

        static void InternalWait(int percent, int waitMilliseconds)
        {
            var currentValue = (int)TotalCpu.NextValue();
            while(currentValue > percent || currentValue == 0)
            {
                Log.Debug($"Waiting {waitMilliseconds} milliseconds for CPU to drop below {percent} percent");
                Thread.Sleep(waitMilliseconds);
                currentValue = (int)TotalCpu.NextValue();
            }
        }

        public static StopwatchExtensions.TimedExecutionSummary Execute
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string timeFormat = DefaultTimeFormat,
             int maxTries = 1,
             [InstantHandle]Action setup = null,
             [InstantHandle]Action tearDown = null)
        {
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            string Format(TimeSpan? date) => date?.ToString(timeFormat) ?? "";

            StopwatchExtensions.TimedExecutionSummary executionSummary = null;

            MachineWideSingleThreaded.Execute(
                () =>
                {
                    for(var tries = 1; tries <= maxTries; tries++)
                    {
                        setup?.Invoke();
                        executionSummary = StopwatchExtensions.TimeExecution(action: action, iterations: iterations);
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
                            WaitUntilCpuLoadIsBelowPercent(50);
                            continue;
                        }
                        PrintSummary(iterations, maxAverage, maxTotal, description, Format, executionSummary);
                        break;
                    }
                });

            return executionSummary;
        }

        public static StopwatchExtensions.TimedThreadedExecutionSummary ExecuteThreaded
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             bool timeIndividualExecutions = false,
             string description = "",
             string timeFormat = DefaultTimeFormat,
             [InstantHandle]Action setup = null,
             [InstantHandle]Action tearDown = null,
             int maxTries = 1)
        {
            StopwatchExtensions.TimedThreadedExecutionSummary executionSummary = null;

            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            // ReSharper disable AccessToModifiedClosure

            string Format(TimeSpan? date) => date?.ToString(timeFormat) ?? "";

            void PrintResults()
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

            MachineWideSingleThreaded.Execute(
                () =>
                {
                    for(int tries = 1; tries <= maxTries; tries++)
                    {
                        setup?.Invoke();
                        executionSummary = StopwatchExtensions.TimeExecutionThreaded(action: action, iterations: iterations, timeIndividualExecutions: timeIndividualExecutions);
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
                                PrintResults();
                                throw;
                            }
                            WaitUntilCpuLoadIsBelowPercent(50);
                            continue;
                        }
                        PrintResults();
                        break;
                    }
                });

            return executionSummary;
        }

        static void RunAsserts(TimeSpan? maxAverage, TimeSpan? maxTotal, StopwatchExtensions.TimedExecutionSummary executionSummary, [InstantHandle]Func<TimeSpan?, string> format)
        {
            if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
            {
                throw new TimeOutException($"{nameof(maxTotal)}: {format(maxTotal)} exceeded. Was: {format(executionSummary.Total)}");
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
                    $@"Executed {iterations} iterations of {description}  
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)} 
    Average: {format
                        (executionSummary.Average)} Limit: {format(maxAverage)}");
            }
            else
            {
                SafeConsole.WriteLine(
                    $@"Executed {iterations} iterations of {description}  
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)}");
            }
        }
    }
}