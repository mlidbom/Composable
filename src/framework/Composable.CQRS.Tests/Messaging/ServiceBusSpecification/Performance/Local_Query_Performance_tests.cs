using System;
using Composable.DependencyInjection;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture, Performance, Serial] public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test] public void Runs_10_000__MultiThreaded_local_requests_making_1_local_query_each_in_10_milliseconds() =>
            RunScenario(threaded: true, requests: 10_000, queriesPerRequest: 1, maxTotal: 10.Milliseconds().NCrunchSlowdownFactor(15));

        [Test] public void Runs_10_000_SingleThreaded_local_requests_making_a_single_local_query_in_26_milliseconds() =>
            RunScenario(threaded: false, requests: 10_000, queriesPerRequest: 1, maxTotal: 26.Milliseconds().NCrunchSlowdownFactor(5));

        [Test] public void Runs_1_000__MultiThreaded_local_requests_making_10_local_queríes_each_in_7_milliseconds() =>
            RunScenario(threaded: true, requests: 1_000, queriesPerRequest: 10, maxTotal: 7.Milliseconds().NCrunchSlowdownFactor(11));

        [Test] public void Runs_1_000__SingleThreaded_local_requests_making_10_local_queríes_each_in_10_milliseconds() =>
            RunScenario(threaded: false, requests: 1_000, queriesPerRequest: 10, maxTotal: 10.Milliseconds().NCrunchSlowdownFactor(5));

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

