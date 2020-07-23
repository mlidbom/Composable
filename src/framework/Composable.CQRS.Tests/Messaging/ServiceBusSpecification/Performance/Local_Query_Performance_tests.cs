using System;
using Composable.DependencyInjection;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.Testing;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [Performance, Serial] public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test, Serial] public void Runs_10_000__MultiThreaded_local_requests_making_a_single_local_query_each_in_15_milliseconds() =>
            RunScenario(threaded: true, requests: 10_000.IfInstrumentedDivideBy(12), queriesPerRequest: 1, maxTotal: 15.Milliseconds());

        [Test, Serial] public void Runs_10_000_SingleThreaded_local_requests_making_a_single_local_query_each_in_40_milliseconds() =>
            RunScenario(threaded: false, requests: 10_000.IfInstrumentedDivideBy(6), queriesPerRequest: 1, maxTotal: 40.Milliseconds());

        [Test, Serial] public void Runs_10_000__MultiThreaded_local_requests_making_10_local_queries_each_in_30_milliseconds() =>
            RunScenario(threaded: true, requests: 10_000.IfInstrumentedDivideBy(12), queriesPerRequest: 10, maxTotal: 30.Milliseconds());

        [Test, Serial] public void Runs_10_000__SingleThreaded_local_requests_making_10_local_queries_each_in_150_milliseconds() =>
            RunScenario(threaded: false, requests: 10_000.IfInstrumentedDivideBy(6), queriesPerRequest: 10, maxTotal: 150.Milliseconds());

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

