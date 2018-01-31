using Composable.DependencyInjection;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture, Performance, Serial] public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test] public void Runs_100_000_MultiThreaded_local_queries_in_100_milliSeconds()
        {
            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: 10_000);

            TimeAsserter.ExecuteThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: 100_000, maxTotal: 100.Milliseconds().NCrunchSlowdownFactor(1));
        }

        [Test] public void Runs_100_000_SingleThreaded_local_queries_in_270_milliseconds()
        {
            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: 10);

            TimeAsserter.Execute(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: 100_000, maxTotal: 270.Milliseconds().NCrunchSlowdownFactor(1));
        }
    }
}
