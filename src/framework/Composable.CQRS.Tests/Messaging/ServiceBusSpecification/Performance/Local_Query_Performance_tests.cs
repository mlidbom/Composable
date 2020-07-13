using System;
using Composable.DependencyInjection;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.System;
using Composable.Testing;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture, Performance, Serial] public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test, Serial] public void Runs_10_000__MultiThreaded_local_requests_making_a_single_local_query_each_in_25_milliseconds() =>
            RunScenario(threaded: true, requests: 10_000.InstrumentationSlowdown(12), queriesPerRequest: 1, maxTotal: 25.Milliseconds());

        [Test, Serial] public void Runs_10_000_SingleThreaded_local_requests_making_a_single_local_query_in_50_milliseconds() =>
            RunScenario(threaded: false, requests: 10_000.InstrumentationSlowdown(6), queriesPerRequest: 1, maxTotal: 50.Milliseconds());

        [Test, Serial] public void Runs_10_000__MultiThreaded_local_requests_making_10_local_queries_each_in_70_milliseconds() =>
            RunScenario(threaded: true, requests: 10_000.InstrumentationSlowdown(11), queriesPerRequest: 10, maxTotal: 70.Milliseconds());

        [Test, Serial] public void Runs_1_000__SingleThreaded_local_requests_making_10_local_queries_each_in_15_milliseconds() =>
            RunScenario(threaded: false, requests: 1_000.InstrumentationSlowdown(5), queriesPerRequest: 10, maxTotal: 15.Milliseconds());

        void RunScenario(bool threaded, int requests, int queriesPerRequest, TimeSpan maxTotal)
        {
            //ncrunch: no coverage start
            void RunRequest()
            {
                ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() =>
                {
                    for(int i = 0; i < queriesPerRequest; i++)
                    {
                        ServerBusSession.Execute(new MyLocalQuery());
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
    }
}

