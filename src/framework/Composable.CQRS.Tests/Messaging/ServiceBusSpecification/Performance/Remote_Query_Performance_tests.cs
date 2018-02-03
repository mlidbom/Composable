using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture, Performance, Serial] public class RemoteQueryPerformanceTests : PerformanceTestBase
    {
        [Test, Serial] public void MultiThreaded_Runs_1000_local_requests_making_one_remote_query_each_in_130_milliSeconds() =>
            RunScenario(threaded: true, requests: 1000.InstrumentationSlowdown(2.0), queriesPerRequest: 1, maxTotal: 130.Milliseconds());

        [Test, Serial] public void SingleThreaded_Runs_1000_local_requests_making_one_remote_query_each_in_500_milliSeconds() =>
            RunScenario(threaded: false, requests: 1000.InstrumentationSlowdown(1.3), queriesPerRequest: 1, maxTotal: 500.Milliseconds());

        [Test, Serial] public void MultiThreaded_Runs_100_local_requests_making_10_remote_queries_each_in_140_milliSeconds() =>
            RunScenario(threaded: true, requests: 100.InstrumentationSlowdown(2.3), queriesPerRequest: 10, maxTotal: 140.Milliseconds());

        [Test, Serial] public void SingleThreaded_Runs_100_local_requests_making_10_remote_queries_each_in_500_milliSeconds() =>
            RunScenario(threaded: false, requests: 100.InstrumentationSlowdown(1.3), queriesPerRequest: 10, maxTotal: 500.Milliseconds());

        [Test, Serial] public void Async_Runs_1_000_local_requests_making_one_async_remote_query_each_in_120_milliSeconds() =>
            RunAsyncScenario(requests: 1_000.InstrumentationSlowdown(2.0), queriesPerRequest: 1, maxTotal: 120.Milliseconds());

        [Test, Serial] public void Async_Runs_100_local_requests_making_10_async_remote_queries_each_in_85_milliSeconds() =>
            RunAsyncScenario(requests: 100.InstrumentationSlowdown(3.0), queriesPerRequest: 10, maxTotal: 85.Milliseconds());


        void RunScenario(bool threaded, int requests, int queriesPerRequest, TimeSpan maxTotal)
        {
            var navigationSpecification = NavigationSpecification.Get(new MyRemoteQuery());

            //ncrunch: no coverage start
            void RunRequest()
            {
                ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() =>
                {
                    for(int i = 0; i < queriesPerRequest; i++)
                    {
                        RemoteNavigator.Navigate(navigationSpecification);
                    }
                });
            }

            //ncrunch: no coverage end

            if(threaded)
            {
                StopwatchExtensions.TimeExecutionThreaded(RunRequest, iterations: requests);
                TimeAsserter.ExecuteThreaded(RunRequest, iterations: requests, maxTotal: maxTotal);
            } else
            {
                StopwatchExtensions.TimeExecution(RunRequest, iterations: requests);
                TimeAsserter.Execute(RunRequest, iterations: requests, maxTotal: maxTotal);
            }
        }

        void RunAsyncScenario(int requests, int queriesPerRequest, TimeSpan maxTotal)
        {
            var navigationSpecification = NavigationSpecification.Get(new MyRemoteQuery());

            //ncrunch: no coverage start
            async Task RunRequestAsync()
            {
                await ClientEndpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(
                    async () => await Task.WhenAll(1.Through(queriesPerRequest)
                                                    .Select(index => RemoteNavigator.NavigateAsync(navigationSpecification))
                                                    .ToArray()));
            }

            async Task RunScenario() => await Task.WhenAll(1.Through(requests).Select(_ => RunRequestAsync()).ToArray());
            //ncrunch: no coverage end

            RunScenario().Wait();

            TimeAsserter.Execute(() => RunScenario().Wait(), maxTotal: maxTotal);
        }
    }
}
