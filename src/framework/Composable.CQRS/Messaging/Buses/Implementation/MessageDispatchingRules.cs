using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.Messaging.Buses.Implementation
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IReadOnlyList<IMessage> executingMessages, IMessage message)
        {
            if(!(message is IQuery)) return true;

            return executingMessages.None(executing => executing is IEvent || executing is ICommand);
        }
    }

    class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IReadOnlyList<IMessage> executingMessages, IMessage message)
        {
            if(message is IQuery) return true;

            return executingMessages.None(executing => executing is IEvent || executing is ICommand);
        }
    }
}
