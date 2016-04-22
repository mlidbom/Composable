using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Composable.System.Linq;
using FluentAssertions;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public static class TimeAsserter
    {
        public class TimedExecutionSummary
        {
            public TimedExecutionSummary(int iterations, TimeSpan total)
            {
                Iterations = iterations;
                Total = total;
            }

            public int Iterations { get; }
            public TimeSpan Total { get; }
            public TimeSpan Average => ((int)Total.TotalMilliseconds/Iterations).Milliseconds();
        }

        public static TimedExecutionSummary Execute
            (Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             bool parallellize = false,
             string description = "",
             string timeFormat = "ss\\.fff")
        {
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            var total = TimeAction(
                () =>
                {
                    if(parallellize)
                    {
                        var tasks = 1.Through(iterations).Select(_ => Task.Factory.StartNew(action)).ToArray();
                        Task.WaitAll(tasks);
                    }
                    else
                    {
                        for(var i = 0; i < iterations; i++)
                        {
                            action();
                        }
                    }
                });

            var executionSummary = new TimedExecutionSummary(iterations, total);

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";

            if(iterations > 1)
            {
                Console.WriteLine(
                    $@"Executed {iterations:N} iterations of {description}  
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)} 
    Average: {format(executionSummary.Average)} Limit: {format(maxAverage)}");
            }
            else
            {
                Console.WriteLine($@"Executed {iterations} iterations of {description}  
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)}");
            }

            if(maxTotal.HasValue)
            {
                executionSummary.Total.Should().BeLessOrEqualTo(maxTotal.Value, $"{nameof(maxTotal)} exceeded");
            }

            if (maxAverage.HasValue)
            {
                executionSummary.Average.Should().BeLessOrEqualTo(maxAverage.Value, $"{nameof(maxAverage)} exceeded");
            }
            return executionSummary;
        }

        public static TimeSpan TimeAction(Action action)
        {
            var watch = Stopwatch.StartNew();
            action();
            return watch.Elapsed;
        }
    }
}