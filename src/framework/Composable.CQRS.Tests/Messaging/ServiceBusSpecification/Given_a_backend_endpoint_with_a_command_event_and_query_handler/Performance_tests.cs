using Composable.Messaging;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Performance_tests : Fixture
    {
        [Fact] public void Given_30_client_threads_Runs_1000_remote_queries_in_300_milliSecond()
        {
            var navigationSpecification = NavigationSpecification.GetRemote(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientBus.Execute(navigationSpecification), iterations: 100, maxDegreeOfParallelism: 30);

            TimeAsserter.ExecuteThreaded(action: () => ClientBus.Execute(navigationSpecification), iterations: 1000, maxTotal: 300.Milliseconds(), maxDegreeOfParallelism: 30);
        }

        [Fact] public void Given_1_client_thread_Runs_1000_remote_queries_in_1_seconds()
        {
            var navigationSpecification = NavigationSpecification.GetRemote(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientBus.Execute(navigationSpecification), iterations: 100, maxDegreeOfParallelism: 30);

            TimeAsserter.Execute(action: () => ClientBus.Execute(navigationSpecification), iterations: 1000, maxTotal: 1.Seconds());
        }
    }
}
