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
        public static void Execute
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

            var watch = Stopwatch.StartNew();


            if(parallellize)
            {
                var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(action)).ToArray();
                Task.WaitAll(tasks);
            }
            else
            {
                for (var i = 0; i < iterations; i++)
                {
                    action();
                }
            }


            var total = watch.Elapsed;
            var average = TimeSpan.FromMilliseconds((total.TotalMilliseconds/iterations));

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";

            if(iterations > 1)
            {
                Console.WriteLine(
                    $@"Executed {iterations:N} iterations of {description}  
    Total:   {format(total)} Limit: {format(maxTotal)} 
    Average: {format(average)} Limit: {format(maxAverage)}");
            }
            else
            {
                Console.WriteLine($@"Executed {iterations} iterations of {description}  
    Total:   {format(total)} Limit: {format(maxTotal)}");
            }

            if(maxTotal.HasValue)
            {
                total.Should().BeLessOrEqualTo(maxTotal.Value, $"{nameof(maxTotal)} exceeded");
            }

            if (maxAverage.HasValue)
            {
                average.Should().BeLessOrEqualTo(maxAverage.Value, $"{nameof(maxAverage)} exceeded");
            }            
        }
    }
}