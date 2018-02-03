using System;
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
        internal static TimedExecutionSummary TimeExecution([InstantHandle]Action action, int iterations = 1) => MachineWideSingleThreaded.Execute(() =>
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

        internal static TimedThreadedExecutionSummary TimeExecutionThreaded([InstantHandle] Action action, int iterations = 1, bool timeIndividualExecutions = false, int maxDegreeOfParallelism = -1) => MachineWideSingleThreaded.Execute(() =>
        {
            maxDegreeOfParallelism = maxDegreeOfParallelism == -1
                                         ? Math.Max(Environment.ProcessorCount, 8) / 2
                                         : maxDegreeOfParallelism;

            var executionTimes = new List<TimeSpan>();
            TimeSpan TimedAction() => TimeExecution(action);

            var total = TimeExecution(
                () =>
                {
                    if(timeIndividualExecutions)
                    {
                        var tasks = 1.Through(iterations).Select(_ => Task.Factory.StartNew(TimedAction, TaskCreationOptions.LongRunning)).ToArray();
                        // ReSharper disable once CoVariantArrayConversion
                        Task.WaitAll(tasks);
                        executionTimes = tasks.Select(@this => @this.Result).ToList();
                    } else
                    {
                        Parallel.For(fromInclusive: 0,
                                     toExclusive: iterations,
                                     body: index => action(),
                                     parallelOptions: new ParallelOptions()
                                                      {
                                                          MaxDegreeOfParallelism = maxDegreeOfParallelism
                                                      });
                    }
                });

            return new TimedThreadedExecutionSummary(iterations, executionTimes, total);
        });

        internal class TimedExecutionSummary
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

        internal class TimedThreadedExecutionSummary : TimedExecutionSummary
        {
            public TimedThreadedExecutionSummary(int iterations, IReadOnlyList<TimeSpan> individualExecutionTimes, TimeSpan total): base(iterations, total) => IndividualExecutionTimes = individualExecutionTimes;

            public IReadOnlyList<TimeSpan> IndividualExecutionTimes { get; }
        }
    }
}