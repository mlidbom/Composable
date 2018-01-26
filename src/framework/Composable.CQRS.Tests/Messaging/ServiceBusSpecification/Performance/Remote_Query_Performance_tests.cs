using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture] public class RemoteQueryPerformanceTests : PerformanceTestBase
    {
        [Test] public void Given_30_client_threads_Runs_100_remote_queries_in_50_milliSecond()
        {
            var navigationSpecification = RemoteNavigationSpecification.GetRemote(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.ExecuteRemoteOn(ClientBusSession)), iterations: 10, maxDegreeOfParallelism: 30);

            TimeAsserter.ExecuteThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.ExecuteRemoteOn(ClientBusSession)), iterations: 100, maxTotal: 50.Milliseconds(), maxDegreeOfParallelism: 30);
        }

        [Test] public void Given_1_client_thread_Runs_100_remote_queries_in_100_milliseconds()
        {
            var navigationSpecification = RemoteNavigationSpecification.GetRemote(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.ExecuteRemoteOn(ClientBusSession)), iterations: 10, maxDegreeOfParallelism: 30);

            TimeAsserter.Execute(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.ExecuteRemoteOn(ClientBusSession)), iterations: 100, maxTotal: 100.Milliseconds());
        }
    }
}
