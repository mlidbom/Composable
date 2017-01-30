using System;
using System.Diagnostics;
using Composable.System;
using Composable.System.Diagnostics;

namespace Composable.Testing
{
    internal static class TimeAsserter
    {
        private const string DefaultTimeFormat = "ss\\.fff";

        public static StopwatchExtensions.TimedExecutionSummary Execute
            (Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string timeFormat = DefaultTimeFormat, 
             int maxTries = 1)
        {
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";
            StopwatchExtensions.TimedExecutionSummary executionSummary = null;
            for(int tries = 1; tries <= maxTries; tries++)
            {
                executionSummary = StopwatchExtensions.TimeExecution(action: action, iterations: iterations);                                
                try
                {
                    RunAsserts(maxAverage: maxAverage, maxTotal: maxTotal, executionSummary: executionSummary, format:format);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Try: {tries} {e.GetType().FullName}: {e.Message}");
                    if(tries >= maxTries)
                    {
                        PrintSummary(iterations, maxAverage, maxTotal, description, format, executionSummary);
                        throw;
                    }
                    continue;
                }
                PrintSummary(iterations, maxAverage, maxTotal, description, format, executionSummary);
                break;
            }

            return executionSummary;
        }

        public static StopwatchExtensions.TimedThreadedExecutionSummary ExecuteThreaded
            (Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             bool timeIndividualExecutions = false,
             string description = "",
             string timeFormat = DefaultTimeFormat,
             int maxTries = 1)
        {
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            StopwatchExtensions.TimedThreadedExecutionSummary executionSummary = null;

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";

            Action printResults = () =>
                                  {
                                      PrintSummary(iterations, maxAverage, maxTotal, description, format, executionSummary);

                                      if(timeIndividualExecutions)
                                      {
                                          Console.WriteLine(
                                              $@"  
    Individual execution times    
    Average: {format(executionSummary.IndividualExecutionTimes.Average())}
    Min:     {format(executionSummary.IndividualExecutionTimes.Min())}
    Max:     {format(executionSummary.IndividualExecutionTimes.Max())}
    Sum:     {format(executionSummary.IndividualExecutionTimes.Sum())}");
                                      }
                                  };            

            for (int tries = 1; tries <= maxTries; tries++)
            {
                executionSummary = StopwatchExtensions.TimeExecutionThreaded(action: action, iterations: iterations, timeIndividualExecutions: timeIndividualExecutions);
                try
                {
                    RunAsserts(maxAverage, maxTotal, executionSummary, format);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Try: {tries} {e.GetType().FullName}: {e.Message}");
                    if (tries >= maxTries)
                    {
                        printResults();
                        throw;
                    }
                    continue;
                }
                printResults();
                break;
            }

            return executionSummary;
        }

        private static void RunAsserts(TimeSpan? maxAverage, TimeSpan? maxTotal, StopwatchExtensions.TimedExecutionSummary executionSummary, Func<TimeSpan?, string> format)
        {
            if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
            {
                throw new Exception($"{nameof(maxTotal)}: {format(maxTotal)} exceeded. Was: {format(executionSummary.Total)}");
            }

            if(maxAverage.HasValue && executionSummary.Average > maxAverage.Value)
            {
                throw new Exception($"{nameof(maxAverage)} exceeded");
            }
        }
        private static void PrintSummary
            (int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, string description, Func<TimeSpan?, string> format, StopwatchExtensions.TimedExecutionSummary executionSummary)
        {
            if(iterations > 1)
            {
                Console.WriteLine(
                    $@"Executed {iterations} iterations of {description}  
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)} 
    Average: {format
                        (executionSummary.Average)} Limit: {format(maxAverage)}");
            }
            else
            {
                Console.WriteLine(
                    $@"Executed {iterations} iterations of {description}  
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)}");
            }
        }
    }
}