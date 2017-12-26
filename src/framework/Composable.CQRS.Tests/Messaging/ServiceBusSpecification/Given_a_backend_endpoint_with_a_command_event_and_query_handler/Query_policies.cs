using System.Threading.Tasks;
using Composable.Testing.Threading;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Query_policies : Fixture
    {
        [Fact] public async Task The_same_query_can_be_reused_in_parallel_without_issues()
        {
            var test = new MyQuery();

            QueryHandlerThreadGate.Close();

            var result1 = Host.ClientBus.QueryAsync(test); //awaiting later
            var result2 = Host.ClientBus.QueryAsync(test); //awaiting later

            QueryHandlerThreadGate.AwaitQueueLengthEqualTo(length: 2);
            QueryHandlerThreadGate.Open();

            await Task.WhenAll(result1, result2);
        }
    }
}
