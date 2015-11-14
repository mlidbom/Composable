using System;
using System.Diagnostics;
using FluentAssertions;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public static class TimeAsserter
    {
        public static void Execute
            (Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxIndividual = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string timeFormat = "ss\\.fff")
        {
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;
            maxIndividual = maxIndividual != default(TimeSpan) ? maxIndividual : TimeSpan.MaxValue;

            var maxRecorded = 0.Milliseconds();
            var minRecorded = TimeSpan.MaxValue;

            var watch = Stopwatch.StartNew();
            for(var i = 0; i < iterations; i++)
            {
                var individual = Stopwatch.StartNew();
                action();
                maxRecorded = maxRecorded > individual.Elapsed ? maxRecorded : individual.Elapsed;
                minRecorded = minRecorded < individual.Elapsed ? minRecorded : individual.Elapsed;
            }
            var total = watch.Elapsed;
            var average = TimeSpan.FromMilliseconds((total.TotalMilliseconds/iterations));

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";

            if(iterations > 1)
            {
                Console.WriteLine(
                    $@"Executed {iterations} iterations of {description}  
    Total:   {format(total)} Limit: {format(maxTotal)} 
    Min:     {format(minRecorded)}
    Average: {format(average)} Limit: {format(maxAverage)} 
    Max:     {format(maxRecorded)} Limit: {format(maxIndividual)}");
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

            if (maxIndividual.HasValue)
            {
                maxRecorded.Should().BeLessOrEqualTo(maxIndividual.Value, $"{nameof(maxIndividual)} exceeded");
            }
        }
    }
}