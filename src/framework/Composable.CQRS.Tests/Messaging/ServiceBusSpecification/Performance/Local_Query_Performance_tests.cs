using Composable.DependencyInjection;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture, Performance, Isolated, Serial] public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test, Isolated, Serial] public void Runs_100_000_MultiThreaded_local_queries_in_FIXME_milliSeconds()
        {
            const int iterations = 100_000;
            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: iterations);

            TimeAsserter.ExecuteThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: iterations, maxTotal: 85.Milliseconds());
        }

        [Test, Isolated, Serial] public void Runs_100_000_SingleThreaded_local_queries_in_FIXME_milliseconds()
        {
            const int iterations = 100_000;
            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: iterations);

            TimeAsserter.Execute(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: iterations, maxTotal: 212.Milliseconds());
        }
    }
}
