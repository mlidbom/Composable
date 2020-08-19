using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Hypermedia;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.LinqCE;
using Composable.Testing;
using Composable.Testing.Performance;
using NUnit.Framework;
using CreatesItsOwnResultQuery = Composable.Messaging.MessageTypes.Remotable.NonTransactional.Queries.NewableResultLink<Composable.Tests.Messaging.Hypermedia.PerformanceTestBase.MyQueryResult>;

namespace Composable.Tests.Messaging.Hypermedia
{
    public class RemoteQueryPerformanceTests : PerformanceTestBase
    {
        [Test] public void SingleThreaded_Runs_100_local_requests_making_one_remote_query_each_in_60_milliseconds() =>
            RunScenario(threaded: false, requests: 100.EnvDivide(instrumented:1.3), queriesPerRequest: 1, maxTotal: 60.Milliseconds(), query: new MyRemoteQuery());

        [Test] public void SingleThreaded_Runs_100_local_requests_making_one_ICreateMyOwnResult_query_each_in_2_milliseconds() =>
            RunScenario(threaded: false, requests: 100.EnvDivide(instrumented:1.3), queriesPerRequest: 1, maxTotal: 2.Milliseconds(), query: new CreatesItsOwnResultQuery());

        [Test] public void SingleThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_60_milliseconds() =>
            RunScenario(threaded: false, requests: 10.EnvDivide(instrumented:1.3), queriesPerRequest: 10, maxTotal: 60.Milliseconds(), query: new MyRemoteQuery());

        [Test] public void SingleThreaded_Runs_10_local_requests_making_10_ICreateMyOwnResult_query_each_in_1_milliseconds() =>
            RunScenario(threaded: false, requests: 10.EnvDivide(instrumented:1.3), queriesPerRequest: 10, maxTotal: 1.Milliseconds(), query: new CreatesItsOwnResultQuery());

        [Test] public void SingleThreaded_Runs_1_local_request_making_200_ICreateMyOwnResult_query_each_in_1_milliseconds() =>
            RunScenario(threaded: false, requests: 1, queriesPerRequest: 200.EnvDivide(instrumented:1.2), maxTotal: 1.Milliseconds(), query: new CreatesItsOwnResultQuery());

        [Test] public void MultiThreaded_Runs_100_local_requests_making_one_remote_query_each_in_12_milliseconds() =>
            RunScenario(threaded: true, requests: 100.EnvDivide(instrumented:1.5), queriesPerRequest: 1, maxTotal: 12.Milliseconds(), query: new MyRemoteQuery());

        [Test] public void MultiThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_15_milliseconds() =>
            RunScenario(threaded: true, requests: 10.EnvDivide(instrumented:1.5), queriesPerRequest: 10, maxTotal: 15.Milliseconds(), query: new MyRemoteQuery());

        [Test] public void MultiThreaded_Runs_10_local_request_making_200_ICreateMyOwnResult_query_each_in_3_milliseconds() =>
            RunScenario(threaded: true, requests: 10, queriesPerRequest: 200.EnvDivide(instrumented: 1.2), maxTotal: 3.Milliseconds(), query: new CreatesItsOwnResultQuery());

        [Test] public async Task Async_Runs_100_local_requests_making_one_async_remote_query_each_in_10_milliseconds() =>
            await RunAsyncScenario(requests: 100.EnvDivide(instrumented:1.5), queriesPerRequest: 1, maxTotal: 10.Milliseconds(), query: new MyRemoteQuery());

        [Test] public async Task Async_Runs_10_local_requests_making_10_async_remote_queries_each_in_7_milliseconds() =>
            await RunAsyncScenario(requests: 10.EnvDivide(instrumented:1.5), queriesPerRequest: 10, maxTotal: 7.Milliseconds(), query: new MyRemoteQuery());

        [Test] public async Task Async_Runs_10_local_request_making_200_ICreateMyOwnResult_query_each_in_9_milliseconds() =>
            await RunAsyncScenario(requests: 10, queriesPerRequest: 200.EnvDivide(instrumented: 1.5), maxTotal: 9.Milliseconds(), query: new CreatesItsOwnResultQuery());


        void RunScenario(bool threaded, int requests, int queriesPerRequest, TimeSpan maxTotal, MessageTypes.IRemotableQuery<MyQueryResult> query)
        {
            var navigationSpecification = NavigationSpecification.Get(query);

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

        async Task RunAsyncScenario(int requests, int queriesPerRequest, TimeSpan maxTotal, MessageTypes.IRemotableQuery<MyQueryResult> query)
        {
            var navigationSpecification = NavigationSpecification.Get(query);

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
