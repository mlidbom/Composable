using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Composable.System.Linq;

namespace Composable.System.Diagnostics
{
    ///<summary>Extensions to the Stopwatch class and related functionality.</summary>
    public static class StopwatchExtensions
    {
        ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
        public static TimeSpan TimeExecution(Action action) => new Stopwatch().TimeExecution(action); 

        ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
        public static TimeSpan TimeExecution(this Stopwatch @this, Action action)
        {
            @this.Reset();
            @this.Start();
            action();
            return @this.Elapsed;
        }


        public static TimedExecutionSummary TimeExecution(Action action, int iterations = 1)
        {
            var total = TimeExecution(
                () =>
                {
                    for(int i = 0; i < iterations; i++)
                    {
                        action();
                    }
                });

            return new TimedExecutionSummary(iterations, total);
        }

        public static TimedThreadedExecutionSummary TimeExecutionThreaded(Action action, int iterations = 1, bool timeIndividualExecutions = false)
        {
            var executionTimes = new List<TimeSpan>();
            Func<TimeSpan> timedAction = () => TimeExecution(action);

            var total = TimeExecution(
                () =>
                {
                    if(timeIndividualExecutions)
                    {
                        var tasks = 1.Through(iterations).Select(_ => Task.Factory.StartNew(timedAction)).ToArray();
                        // ReSharper disable once CoVariantArrayConversion
                        Task.WaitAll(tasks);
                        executionTimes = tasks.Select(@this => @this.Result).ToList();
                    }
                    else
                    {
                        var tasks = 1.Through(iterations).Select(_ => Task.Factory.StartNew(action)).ToArray();
                        Task.WaitAll(tasks);
                    }
                });

            return new TimedThreadedExecutionSummary(iterations, executionTimes, total);
        }


        public class TimedExecutionSummary
        {
            public TimedExecutionSummary(int iterations, TimeSpan total)
            {
                Iterations = iterations;
                Total = total;
            }

            public int Iterations { get; }
            public TimeSpan Total { get; }
            public TimeSpan Average => (Total.TotalMilliseconds / Iterations).Milliseconds();
        }

        public class TimedThreadedExecutionSummary : TimedExecutionSummary
        {
            public TimedThreadedExecutionSummary(int iterations, IReadOnlyList<TimeSpan> individualExecutionTimes, TimeSpan total): base(iterations, total)
            {
                IndividualExecutionTimes = individualExecutionTimes;
            }

            public IReadOnlyList<TimeSpan> IndividualExecutionTimes { get; }
        }
    }
}