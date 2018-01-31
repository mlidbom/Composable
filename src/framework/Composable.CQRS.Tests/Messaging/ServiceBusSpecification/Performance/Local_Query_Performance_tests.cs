using Composable.DependencyInjection;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture] public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test] public void Runs_10000_MultiThreaded_local_queries_in_30_milliSecond()
        {
            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: 10000);

            TimeAsserter.ExecuteThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: 10000, maxTotal: 30.Milliseconds(), maxTries: 1);
        }

        [Test] public void Runs_10000_SingleThreaded_local_queries_in_30_milliseconds()
        {
            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: 10);

            TimeAsserter.Execute(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ServerBusSession.Execute(new MyLocalQuery())), iterations: 10000, maxTotal: 30.Milliseconds());
        }
    }
}
