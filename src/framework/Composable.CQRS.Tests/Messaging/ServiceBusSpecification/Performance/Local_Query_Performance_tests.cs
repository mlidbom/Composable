using Composable.Messaging;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture] public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test] public void Given_30_client_threads_Runs_1000_local_queries_in_5_milliSecond()
        {
            var navigationSpecification = NavigationSpecification.Get(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerBus.Execute(navigationSpecification), iterations: 10, maxDegreeOfParallelism: 30);

            TimeAsserter.ExecuteThreaded(action: () => ServerBus.Execute(navigationSpecification), iterations: 1000, maxTotal: 5.Milliseconds(), maxDegreeOfParallelism: 30);
        }

        [Test] public void Given_1_client_thread_Runs_1000_local_queries_in_5_milliseconds()
        {
            var navigationSpecification = NavigationSpecification.Get(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ServerBus.Execute(navigationSpecification), iterations: 10, maxDegreeOfParallelism: 30);

            TimeAsserter.Execute(action: () => ServerBus.Execute(navigationSpecification), iterations: 1000, maxTotal: 5.Milliseconds());
        }
    }
}
