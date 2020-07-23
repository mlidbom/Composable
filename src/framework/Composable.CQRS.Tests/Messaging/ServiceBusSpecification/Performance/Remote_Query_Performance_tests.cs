using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Hypermedia;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.LinqCE;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.Testing;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [Performance, Serial] public class RemoteQueryPerformanceTests : PerformanceTestBase
    {
        [Test] public void SingleThreaded_Runs_100_local_requests_making_one_remote_query_each_in_60_milliseconds() =>
            RunScenario(threaded: false, requests: 100.IfInstrumentedDivideBy(1.3), queriesPerRequest: 1, maxTotal: 60.Milliseconds());

        [Test] public void SingleThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_60_milliseconds() =>
            RunScenario(threaded: false, requests: 10.IfInstrumentedDivideBy(1.3), queriesPerRequest: 10, maxTotal: 60.Milliseconds());

        [Test] public void MultiThreaded_Runs_100_local_requests_making_one_remote_query_each_in_12_milliseconds() =>
            RunScenario(threaded: true, requests: 100.IfInstrumentedDivideBy(1.5), queriesPerRequest: 1, maxTotal: 12.Milliseconds());

        [Test] public void MultiThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_15_milliseconds() =>
            RunScenario(threaded: true, requests: 10.IfInstrumentedDivideBy(1.5), queriesPerRequest: 10, maxTotal: 15.Milliseconds());

        [Test] public async Task Async_Runs_100_local_requests_making_one_async_remote_query_each_in_10_milliseconds() =>
            await RunAsyncScenario(requests: 100.IfInstrumentedDivideBy(1.5), queriesPerRequest: 1, maxTotal: 10.Milliseconds());

        [Test] public async Task Async_Runs_10_local_requests_making_10_async_remote_queries_each_in_7_milliseconds() =>
            await RunAsyncScenario(requests: 10.IfInstrumentedDivideBy(1.5), queriesPerRequest: 10, maxTotal: 7.Milliseconds());


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
                StopwatchCE.TimeExecutionThreaded(RunRequest, iterations: requests); //Warmup
                TimeAsserter.ExecuteThreaded(RunRequest, iterations: requests, maxTotal: maxTotal);
            } else
            {
                StopwatchCE.TimeExecution(RunRequest, iterations: requests); //Warmup
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

        public RemoteQueryPerformanceTests(string _) : base(_) {}
    }
}
