using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Composable.System.Linq;
using Composable.System.Threading;
using JetBrains.Annotations;

namespace Composable.System.Diagnostics
{
    ///<summary>Extensions to the Stopwatch class and related functionality.</summary>
    public static class StopwatchExtensions
    {
        static readonly MachineWideSingleThreaded MachineWideSingleThreaded = MachineWideSingleThreaded.For(typeof(StopwatchExtensions));

        ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
        internal static TimeSpan TimeExecution([InstantHandle]Action action) => new Stopwatch().TimeExecution(action);

        ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
        static TimeSpan TimeExecution(this Stopwatch @this, [InstantHandle]Action action)
        {
            @this.Reset();
            @this.Start();
            action();
            return @this.Elapsed;
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
            public TimedThreadedExecutionSummary(int iterations, IReadOnlyList<TimeSpan> individualExecutionTimes, TimeSpan total): base(iterations, total) => IndividualExecutionTimes = individualExecutionTimes;

            public IReadOnlyList<TimeSpan> IndividualExecutionTimes { get; }
        }
    }
}