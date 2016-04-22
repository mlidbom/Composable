using System;
using System.Linq;
using Composable.System;
using Composable.System.Diagnostics;
using FluentAssertions;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public static class TimeAsserter
    {
        private const string DefaultTimeFormat = "ss\\.fff";

        public static StopwatchExtensions.TimedExecutionSummary Execute
            (Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string timeFormat = DefaultTimeFormat)
        {
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            var executionSummary = StopwatchExtensions.TimeExecution(action: action, iterations: iterations);

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";
            PrintSummary(iterations, maxAverage, maxTotal, description, format, executionSummary);
            RunAsserts(maxAverage, maxTotal, executionSummary);

            return executionSummary;
        }

        public static StopwatchExtensions.TimedThreadedExecutionSummary ExecuteThreaded
            (Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             bool timeIndividualExecutions = false,
             string description = "",
             string timeFormat = DefaultTimeFormat)
        {
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            var executionSummary = StopwatchExtensions.TimeExecutionThreaded(action: action, iterations: iterations, timeIndividualExecutions: timeIndividualExecutions);

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";
            PrintSummary(iterations, maxAverage, maxTotal, description, format, executionSummary);

            Console.WriteLine(
                $@"  
    Individual execution times    
    Average: {format(executionSummary.IndividualExecutionTimes.Average())}
    Min:     {format(executionSummary.IndividualExecutionTimes.Min())}
    Max:     {format(executionSummary.IndividualExecutionTimes.Max())}
    Sum:     {format(executionSummary.IndividualExecutionTimes.Sum())}");

            RunAsserts(maxAverage, maxTotal, executionSummary);

            return executionSummary;
        }

        private static void RunAsserts(TimeSpan? maxAverage, TimeSpan? maxTotal, StopwatchExtensions.TimedExecutionSummary executionSummary)
        {
            if(maxTotal.HasValue)
            {
                executionSummary.Total.Should().BeLessOrEqualTo(maxTotal.Value, $"{nameof(maxTotal)} exceeded");
            }

            if(maxAverage.HasValue)
            {
                executionSummary.Average.Should().BeLessOrEqualTo(maxAverage.Value, $"{nameof(maxAverage)} exceeded");
            }
        }
        private static void PrintSummary
            (int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, string description, Func<TimeSpan?, string> format, StopwatchExtensions.TimedExecutionSummary executionSummary)
        {
            if(iterations > 1)
            {
                Console.WriteLine(
                    $@"Executed {iterations:N} iterations of {description}  
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