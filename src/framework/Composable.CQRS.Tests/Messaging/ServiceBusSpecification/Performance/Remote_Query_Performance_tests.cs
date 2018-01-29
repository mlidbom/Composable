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
            var navigationSpecification = NavigationSpecification.Get(new MyRemoteQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.NavigateOn(RemoteNavigator)), iterations: 10);

            TimeAsserter.ExecuteThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.NavigateOn(RemoteNavigator)), iterations: 100, maxTotal: 50.Milliseconds());
        }

        [Test] public void Given_1_client_thread_Runs_100_remote_queries_in_100_milliseconds()
        {
            var navigationSpecification = NavigationSpecification.Get(new MyRemoteQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.NavigateOn(RemoteNavigator)), iterations: 10);

            TimeAsserter.Execute(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => navigationSpecification.NavigateOn(RemoteNavigator)), iterations: 100, maxTotal: 100.Milliseconds());
        }
    }
}
