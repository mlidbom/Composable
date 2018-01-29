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
        [Test] public void MultiThreaded_Runs_100_remote_queries_in_20_milliSecond()
        {
            var navigationSpecification = NavigationSpecification.Get(new MyRemoteQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => RemoteNavigator.Navigate(navigationSpecification)), iterations: 10);

            TimeAsserter.ExecuteThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => RemoteNavigator.Navigate(navigationSpecification)), iterations: 100, maxTotal: 20.Milliseconds().NCrunchSlowdownFactor(2));
        }

        [Test] public void SingleThreaded_Runs_100_remote_queries_in_100_milliseconds()
        {
            var navigationSpecification = NavigationSpecification.Get(new MyRemoteQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => RemoteNavigator.Navigate(navigationSpecification)), iterations: 10);

            TimeAsserter.Execute(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => RemoteNavigator.Navigate(navigationSpecification)), iterations: 100, maxTotal: 100.Milliseconds());
        }
    }
}
