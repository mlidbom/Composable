using System.Threading.Tasks;
using Composable.Messaging.Buses;
using Composable.Testing.Threading;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Query_policies : Fixture
    {
        [Fact] public void The_same_query_can_be_reused_in_parallel_without_issues()
        {
            var test = new MyQuery();

            QueryHandlerThreadGate.Close();

            var(result1, result2) = ClientEndpoint.ExecuteRequest(session => (RemoteNavigator.GetAsync(test), RemoteNavigator.GetAsync(test)));

            QueryHandlerThreadGate.AwaitQueueLengthEqualTo(length: 2);
            QueryHandlerThreadGate.Open();

            Task.WaitAll(result1, result2);
        }
    }
}
