using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Hypermedia;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.System;
using Composable.Testing;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture, Performance, Serial] public class RemoteQueryPerformanceTests : PerformanceTestBase
    {
        [Test, Serial] public void SingleThreaded_Runs_100_local_requests_making_one_remote_query_each_in_50_milliseconds() =>
            RunScenario(threaded: false, requests: 100.IfInstrumentedDivideBy(1.3), queriesPerRequest: 1, maxTotal: 50.Milliseconds());

        [Test, Serial] public void SingleThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_50_milliseconds() =>
            RunScenario(threaded: false, requests: 10.IfInstrumentedDivideBy(1.3), queriesPerRequest: 10, maxTotal: 50.Milliseconds());

        [Test, Serial] public void MultiThreaded_Runs_100_local_requests_making_one_remote_query_each_in_13_milliseconds() =>
            RunScenario(threaded: true, requests: 100.IfInstrumentedDivideBy(2.0), queriesPerRequest: 1, maxTotal: 13.Milliseconds());

        [Test, Serial] public void MultiThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_14_milliseconds() =>
            RunScenario(threaded: true, requests: 10.IfInstrumentedDivideBy(2.3), queriesPerRequest: 10, maxTotal: 14.Milliseconds());

        [Test, Serial] public async Task Async_Runs_100_local_requests_making_one_async_remote_query_each_in_10_milliseconds() =>
            await RunAsyncScenario(requests: 100.IfInstrumentedDivideBy(2.0), queriesPerRequest: 1, maxTotal: 10.Milliseconds());

        [Test, Serial] public async Task Async_Runs_10_local_requests_making_10_async_remote_queries_each_in_10_milliseconds() =>
            await RunAsyncScenario(requests: 10.IfInstrumentedDivideBy(3.0), queriesPerRequest: 10, maxTotal: 10.Milliseconds());


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
                StopwatchExtensions.TimeExecutionThreaded(RunRequest, iterations: requests); //Warmup
                TimeAsserter.ExecuteThreaded(RunRequest, iterations: requests, maxTotal: maxTotal);
            } else
            {
                StopwatchExtensions.TimeExecution(RunRequest, iterations: requests); //Warmup
                TimeAsserter.Execute(RunRequest, iterations: requests, maxTotal: maxTotal);
            }
        }

        async Task RunAsyncScenario(int requests, int queriesPerRequest, TimeSpan maxTotal)
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

            //Warmup
            await RunScenario();

            await TimeAsserter.ExecuteAsync(RunScenario, maxTotal: maxTotal);
        }
    }
}
