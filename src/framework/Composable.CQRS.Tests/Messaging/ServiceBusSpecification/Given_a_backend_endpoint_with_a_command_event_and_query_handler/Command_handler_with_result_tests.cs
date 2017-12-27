using System.Threading.Tasks;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Command_handler_with_result_tests : Fixture
    {
        [Fact] async Task SendAsyncAsync_first_task_returns_before_handler_has_executed()
        {
            CommandHandlerWithResultThreadGate.Close();
            var sentTask = await Host.ClientBus.SendAsyncAsync(new MyCommandWithResult());
            CommandHandlerWithResultThreadGate.Open();
            var result = await sentTask;
        }
    }
}
