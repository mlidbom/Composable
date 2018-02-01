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
        [Test] public void MultiThreaded_Runs_100_local_requests_making_one_remote_query_each_in_12_milliSeconds() =>
            RunScenario(threaded: true, async: false, requests: 100, queriesPerRequest: 1, maxTotal: 12.Milliseconds().InstrumentationSlowdown(2.0));

        [Test] public void SingleThreaded_Runs_100_local_requests_making_one_remote_query_each_in_50_milliSeconds() =>
            RunScenario(threaded: false, async: false, requests: 100, queriesPerRequest: 1, maxTotal: 50.Milliseconds().InstrumentationSlowdown(1.3));

        [Test] public void MultiThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_12_milliSeconds() =>
            RunScenario(threaded: true, async: false, requests: 10, queriesPerRequest: 10, maxTotal: 12.Milliseconds().InstrumentationSlowdown(2.3));

        [Test] public void SingleThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_50_milliSeconds() =>
            RunScenario(threaded: false, async: false, requests: 10, queriesPerRequest: 10, maxTotal: 50.Milliseconds().InstrumentationSlowdown(1.3));

        [Test] public void MultiThreaded_Runs_100_local_requests_making_one_async_remote_query_each_in_12_milliSeconds() =>
            RunScenario(threaded: true, async: true, requests: 100, queriesPerRequest: 1, maxTotal: 12.Milliseconds().InstrumentationSlowdown(2.0));

        [Test] public void SingleThreaded_Runs_100_local_requests_making_one_async_remote_query_each_in_50_milliSeconds() =>
            RunScenario(threaded: false, async: true, requests: 100, queriesPerRequest: 1, maxTotal: 50.Milliseconds().InstrumentationSlowdown(1.3));

        [Test] public void MultiThreaded_Runs_10_local_requests_making_10_async_remote_queries_each_in_8_milliSeconds() =>
            RunScenario(threaded: true, async: true, requests: 10, queriesPerRequest: 10, maxTotal: 8.Milliseconds().InstrumentationSlowdown(3.0));

        [Test] public void SingleThreaded_Runs_10_local_requests_making_10_async_remote_queries_each_in_12_milliSeconds() =>
            RunScenario(threaded: false, async: true, requests: 10, queriesPerRequest: 10, maxTotal: 12.Milliseconds().InstrumentationSlowdown(2.0));


        void RunScenario(bool threaded, bool async, int requests, int queriesPerRequest, TimeSpan maxTotal)
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

            void RunRequestAsync()
            {
                ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() =>
                {
                    var tasks = 1.Through(queriesPerRequest).Select(index => RemoteNavigator.NavigateAsync(navigationSpecification)).Cast<Task>().ToArray();
                    Task.WaitAll(tasks);
                });
            }

            var runScenario = async ? (Action)RunRequestAsync : RunRequest;

            //ncrunch: no coverage end

            if(threaded)
            {
                StopwatchExtensions.TimeExecutionThreaded(runScenario, iterations: requests);
                TimeAsserter.ExecuteThreaded(runScenario, iterations: requests, maxTotal: maxTotal);
            } else
            {
                StopwatchExtensions.TimeExecution(runScenario, iterations: requests);
                TimeAsserter.Execute(runScenario, iterations: requests, maxTotal: maxTotal);
            }
        }
    }
}
