using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.Messaging.Buses.Implementation
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IReadOnlyList<BusApi.IMessage> executingMessages, BusApi.IMessage message)
        {
            if(!(message is BusApi.IQuery)) return true;

            return executingMessages.None(executing => executing is BusApi.IEvent || executing is BusApi.ICommand);
        }
    }

    class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IReadOnlyList<BusApi.IMessage> executingMessages, BusApi.IMessage message)
        {
            if(message is BusApi.IQuery) return true;

            return executingMessages.None(executing => executing is BusApi.IEvent || executing is BusApi.ICommand);
        }
    }
}
