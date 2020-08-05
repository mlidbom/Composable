using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Logging;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.ThreadingCE;
using JetBrains.Annotations;

namespace Composable.Testing.Performance
{
    public static class TimeAsserter
    {
        const int MaxTriesLimit = 10;
        const int MaxTriesDefault = 10;

        public static async Task<StopwatchCE.TimedExecutionSummary> ExecuteAsync([InstantHandle] Func<Task> action,
                                                                                 int iterations = 1,
                                                                                 TimeSpan? maxAverage = null,
                                                                                 TimeSpan? maxTotal = null,
                                                                                 string description = "",
                                                                                 uint maxTries = MaxTriesDefault,
                                                                                 [InstantHandle] Action? setup = null,
                                                                                 [InstantHandle] Action? tearDown = null)
        {
            HandleArguments(ref maxAverage, ref maxTotal, ref maxTries);

            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                using var _ = DisposableCE.Create(() => tearDown?.Invoke());

                var executionSummary = await StopwatchCE.TimeExecutionAsync(action: action, iterations: iterations).NoMarshalling();

                if(!RunAsserts(executionSummary, maxAverage: maxAverage, maxTotal: maxTotal, maxTries: maxTries, tries: tries))
                    continue;

                PrintSummary(executionSummary, description, iterations, maxAverage, maxTotal);
                return executionSummary;
            }

            throw new Exception("Unreachable");
        }

        public static StopwatchCE.TimedExecutionSummary Execute([InstantHandle] Action action,
                                                                int iterations = 1,
                                                                TimeSpan? maxAverage = null,
                                                                TimeSpan? maxTotal = null,
                                                                string description = "",
                                                                uint maxTries = MaxTriesDefault,
                                                                [InstantHandle] Action? setup = null,
                                                                [InstantHandle] Action? tearDown = null)
        {
            HandleArguments(ref maxAverage, ref maxTotal, ref maxTries);

            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                using var _ = DisposableCE.Create(() => tearDown?.Invoke());

                var executionSummary = StopwatchCE.TimeExecution(action: action, iterations: iterations);

                if(!RunAsserts(executionSummary, maxAverage: maxAverage, maxTotal: maxTotal, maxTries: maxTries, tries: tries))
                    continue;

                PrintSummary(executionSummary, description, iterations, maxAverage, maxTotal);
                return executionSummary;
            }

            throw new Exception("Unreachable");
        }

        public static StopwatchCE.TimedThreadedExecutionSummary ExecuteThreaded([InstantHandle] Action action,
                                                                                int iterations = 1,
                                                                                TimeSpan? maxAverage = null,
                                                                                TimeSpan? maxTotal = null,
                                                                                string description = "",
                                                                                [InstantHandle] Action? setup = null,
                                                                                [InstantHandle] Action? tearDown = null,
                                                                                uint maxTries = MaxTriesDefault,
                                                                                int maxDegreeOfParallelism = -1)
        {
            HandleArguments(ref maxAverage, ref maxTotal, ref maxTries);

            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                using var _ = DisposableCE.Create(() => tearDown?.Invoke());

                var executionSummary = StopwatchCE.TimeExecutionThreaded(action: action, iterations: iterations, maxDegreeOfParallelism: maxDegreeOfParallelism);

                if(!RunAsserts(executionSummary, maxAverage: maxAverage, maxTotal: maxTotal, maxTries: maxTries, tries: tries))
                    continue;

                PrintSummary(executionSummary, description, iterations, maxAverage, maxTotal);
                return executionSummary;
            }

            throw new Exception("Unreachable");
        }

        public static StopwatchCE.TimedExecutionSummary ExecuteThreadedLowOverhead([InstantHandle] Action action,
                                                                                   int iterations = 1,
                                                                                   TimeSpan? maxAverage = null,
                                                                                   TimeSpan? maxTotal = null,
                                                                                   string description = "",
                                                                                   [InstantHandle] Action? setup = null,
                                                                                   [InstantHandle] Action? tearDown = null,
                                                                                   uint maxTries = MaxTriesDefault,
                                                                                   int maxDegreeOfParallelism = -1)
        {
            HandleArguments(ref maxAverage, ref maxTotal, ref maxTries);

            for(var tries = 1; tries <= maxTries; tries++)
            {
                setup?.Invoke();
                using var _ = DisposableCE.Create(() => tearDown?.Invoke());
                var executionSummary = StopwatchCE.TimeExecutionThreadedLowOverhead(action: action, iterations: iterations, maxDegreeOfParallelism: maxDegreeOfParallelism);

                if(!RunAsserts(executionSummary, maxAverage: maxAverage, maxTotal: maxTotal, maxTries: maxTries, tries: tries))
                    continue;

                PrintSummary(executionSummary, description, iterations, maxAverage, maxTotal);
                return executionSummary;
            }

            throw new Exception("Unreachable");
        }

        static void HandleArguments(ref TimeSpan? maxAverage, ref TimeSpan? maxTotal, ref uint maxTries)
        {
            Assert.Argument.Assert(maxTries > 0);
            maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
            maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
            TestEnv.Performance.LogMachineSlownessAdjustment();
            maxTries = Math.Min(MaxTriesLimit, maxTries);
        }

        static bool RunAsserts(StopwatchCE.TimedExecutionSummary executionSummary, TimeSpan? maxAverage, TimeSpan? maxTotal, uint maxTries, int tries)
        {
            string failureMessage = "";
            if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
            {
                failureMessage = $"{Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {maxTotal.FormatReadable()}";
            } else if(maxAverage.HasValue && executionSummary.Average > maxAverage.Value)
            {
                failureMessage = $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {maxAverage.FormatReadable()}";
            }

            if(failureMessage.Length == 0) return true;
            if(tries < maxTries) return false;

            throw new TimeOutException(failureMessage);
        }

        public class TimeOutException : Exception
        {
            public TimeOutException(string message) : base(message) {}
        }

        static string Percent(TimeSpan percent, TimeSpan of) => $"{(int)((percent.TotalMilliseconds / of.TotalMilliseconds) * 100)}%";

        static void PrintSummary(StopwatchCE.TimedExecutionSummary executionSummary, string description, int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal)
        {
            string maxAverageReport = maxAverage == null
                                          ? ""
                                          : $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {maxAverage.FormatReadable()}";

            string maxTotalReport = maxTotal == null
                                        ? ""
                                        : $" {Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {maxTotal.FormatReadable()}";

            ConsoleCE.WriteImportantLine($@"""{description}"" {iterations:### ### ###} {iterations.Pluralize("iteration")}");
            if(iterations > 1)
            {
                ConsoleCE.WriteLine($@"
Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport}
Average: {executionSummary.Average.FormatReadable()} {maxAverageReport}"
                                       .RemoveLeadingLineBreak());
            } else
            {
                ConsoleCE.WriteLine($@"Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport} ");
            }

            if(executionSummary is StopwatchCE.TimedThreadedExecutionSummary threadedSummary)
            {
                ConsoleCE.WriteLine($@"  
Individual execution times    
    Average: {threadedSummary.IndividualExecutionTimes.Average().FormatReadable()}
    Min:     {threadedSummary.IndividualExecutionTimes.Min().FormatReadable()}
    Max:     {threadedSummary.IndividualExecutionTimes.Max().FormatReadable()}
    Sum:     {threadedSummary.IndividualExecutionTimes.Sum().FormatReadable()}
");
            }
        }
    }
}
