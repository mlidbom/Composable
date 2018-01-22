using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture] public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test] public void Given_30_client_threads_Runs_100_local_queries_in_2_milliSecond()
        {
            var navigationSpecification = NavigationSpecification.Get(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.ExecuteOn(ServerBusSession)), iterations: 10, maxDegreeOfParallelism: 30);

            TimeAsserter.ExecuteThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.ExecuteOn(ServerBusSession)), iterations: 100, maxTotal: 2.Milliseconds(), maxDegreeOfParallelism: 30);
        }

        [Test] public void Given_1_client_thread_Runs_100_local_queries_in_1_milliseconds()
        {
            var navigationSpecification = NavigationSpecification.Get(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.ExecuteOn(ServerBusSession)), iterations: 10, maxDegreeOfParallelism: 30);

            TimeAsserter.Execute(action: () => ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.ExecuteOn(ServerBusSession)), iterations: 100, maxTotal: 1.Milliseconds());
        }
    }
}
