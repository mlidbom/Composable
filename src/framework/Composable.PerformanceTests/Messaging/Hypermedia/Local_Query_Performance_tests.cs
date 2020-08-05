using System;
using Composable.DependencyInjection;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.Testing;
using Composable.Testing.Performance;
using NUnit.Framework;

namespace Composable.Tests.Messaging.Hypermedia
{
    public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test] public void Runs_10_000__MultiThreaded_local_requests_making_a_single_local_query_each_in_20_milliseconds() =>
            RunScenario(threaded: true, requests: 10_000.EnvDivide(instrumented:12), queriesPerRequest: 1, maxTotal: 20.Milliseconds());

        [Test] public void Runs_10_000_SingleThreaded_local_requests_making_a_single_local_query_each_in_80_milliseconds() =>
            RunScenario(threaded: false, requests: 10_000.EnvDivide(instrumented:6), queriesPerRequest: 1, maxTotal: 80.Milliseconds());

        [Test] public void Runs_10_000__MultiThreaded_local_requests_making_10_local_queries_each_in_50_milliseconds() =>
            RunScenario(threaded: true, requests: 10_000.EnvDivide(instrumented:12), queriesPerRequest: 10, maxTotal: 50.Milliseconds());

        [Test] public void Runs_10_000__SingleThreaded_local_requests_making_10_local_queries_each_in_170_milliseconds() =>
            RunScenario(threaded: false, requests: 10_000.EnvDivide(instrumented:6), queriesPerRequest: 10, maxTotal: 170.Milliseconds());

        void RunScenario(bool threaded, int requests, int queriesPerRequest, TimeSpan maxTotal)
        {
            //ncrunch: no coverage start
            void RunRequest()
            {
                ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() =>
                {
                    for(int i = 0; i < queriesPerRequest; i++)
                    {
                        LocalNavigator.Execute(new MyLocalQuery());
                    }
                });
            }
            //ncrunch: no coverage end

            if(threaded)
            {
                StopwatchCE.TimeExecutionThreaded(RunRequest, iterations: requests);
                TimeAsserter.ExecuteThreaded(RunRequest, iterations: requests, maxTotal: maxTotal);
            } else
            {
                StopwatchCE.TimeExecution(RunRequest, iterations: requests);
                TimeAsserter.Execute(RunRequest, iterations: requests, maxTotal: maxTotal);
            }
        }

        public Local_Query_performance_tests(string _) : base(_) {}
    }
}

