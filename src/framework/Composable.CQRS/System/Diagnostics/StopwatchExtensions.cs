using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.System.Linq;
using Composable.System.Threading;
using JetBrains.Annotations;
using TaskExtensions = Composable.System.Threading.TaskExtensions;

namespace Composable.System.Diagnostics
{
    ///<summary>Extensions to the Stopwatch class and related functionality.</summary>
    public static class StopwatchExtensions
    {
        static readonly MachineWideSingleThreaded MachineWideSingleThreaded = MachineWideSingleThreaded.For(typeof(StopwatchExtensions));

        ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
        internal static TimeSpan TimeExecution([InstantHandle]Action action) => new Stopwatch().TimeExecution(action);
        internal static async Task<TimeSpan> TimeExecutionAsync([InstantHandle]Func<Task> action) => await new Stopwatch().TimeExecutionAsync(action).NoMarshalling();

        ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
        static TimeSpan TimeExecution(this Stopwatch @this, [InstantHandle]Action action)
        {
            @this.Reset();
            @this.Start();
            action();
            return @this.Elapsed;
        }

        ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
        static async Task<TimeSpan> TimeExecutionAsync(this Stopwatch @this, [InstantHandle]Func<Task> action)
        {
            @this.Reset();
            @this.Start();
            await action().NoMarshalling();
            return @this.Elapsed;
        }


        //urgent: Can we get MachineWideSingleThreaded functionality with async?
        // ReSharper disable once MethodOverloadWithOptionalParameter
        public static async Task<TimedExecutionSummary> TimeExecutionAsync([InstantHandle]Func<Task> action, int iterations = 1)
        {
            var total = await TimeExecutionAsync(
                async () =>
                {
                    for(var i = 0; i < iterations; i++)
                    {
                        await action().NoMarshalling();
                    }
                }).NoMarshalling();

            return new TimedExecutionSummary(iterations, total);
        }

        // ReSharper disable once MethodOverloadWithOptionalParameter
        public static TimedExecutionSummary TimeExecution([InstantHandle]Action action, int iterations = 1) => MachineWideSingleThreaded.Execute(() =>
        {
            var total = TimeExecution(
                () =>
                {
                    for(var i = 0; i < iterations; i++)
                    {
                        action();
                    }
                });

            return new TimedExecutionSummary(iterations, total);
        });

        public static TimedThreadedExecutionSummary TimeExecutionThreaded([InstantHandle] Action action, int iterations = 1, int maxDegreeOfParallelism = -1) => MachineWideSingleThreaded.Execute(() =>
        {
            maxDegreeOfParallelism = maxDegreeOfParallelism == -1
                                         ? Math.Max(Environment.ProcessorCount, 8) / 2
                                         : maxDegreeOfParallelism;

            TimeSpan TimedAction() => TimeExecution(action);
            var individual = new ConcurrentStack<TimeSpan>();

            var total = TimeExecution(
                () => Parallel.For(fromInclusive: 0,
                                   toExclusive: iterations,
                                   body: index => individual.Push(TimedAction()),
                                   parallelOptions: new ParallelOptions
                                                    {
                                                        MaxDegreeOfParallelism = maxDegreeOfParallelism
                                                    }));

            return new TimedThreadedExecutionSummary(iterations, individual.ToList(), total);
        });

        public class TimedExecutionSummary
        {
            public TimedExecutionSummary(int iterations, TimeSpan total)
            {
                Iterations = iterations;
                Total = total;
            }

            int Iterations { get; }
            public TimeSpan Total { get; }
            public TimeSpan Average => (Total.TotalMilliseconds / Iterations).Milliseconds();
        }

        public class TimedThreadedExecutionSummary : TimedExecutionSummary
        {
            readonly string _description;
            public TimedThreadedExecutionSummary(int iterations, IReadOnlyList<TimeSpan> individualExecutionTimes, TimeSpan total, string description = ""): base(iterations, total)
            {
                _description = description;
                IndividualExecutionTimes = individualExecutionTimes;
            }

            public IReadOnlyList<TimeSpan> IndividualExecutionTimes { get; }

            public override string ToString() =>  $@"
{_description}
Total: {Format(Total)}
Average: {Format(Total)}

Individual execution times    
    Average: {Format(IndividualExecutionTimes.Average())}
    Min:     {Format(IndividualExecutionTimes.Min())}
    Max:     {Format(IndividualExecutionTimes.Max())}
    Sum:     {Format(IndividualExecutionTimes.Sum())}
";

            static string Format(TimeSpan? average) => average?.ToString(@"ss\.ffffff", CultureInfo.InvariantCulture) ?? "";
        }
    }
}