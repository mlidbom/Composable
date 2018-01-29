using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.Messaging.Buses.Implementation
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IReadOnlyList<TransportMessage.InComing> executingMessages, TransportMessage.InComing message)
        {
            if(!(message.IsOfType<BusApi.IQuery>())) return true;

            return executingMessages.None(executing => executing.IsOfType<BusApi.IEvent>() || executing.IsOfType<BusApi.ICommand>());
        }
    }

    class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IReadOnlyList<TransportMessage.InComing> executingMessages, TransportMessage.InComing message)
        {
            if(message.IsOfType<BusApi.IQuery>()) return true;

            return executingMessages.None(executing => executing.IsOfType<BusApi.IEvent>() || executing.IsOfType<BusApi.ICommand>());
        }
    }
}
