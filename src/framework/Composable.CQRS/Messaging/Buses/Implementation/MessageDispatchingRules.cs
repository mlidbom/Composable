using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.Messaging.Buses.Implementation
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IReadOnlyList<MessagingApi.IMessage> executingMessages, MessagingApi.IMessage message)
        {
            if(!(message is MessagingApi.IQuery)) return true;

            return executingMessages.None(executing => executing is MessagingApi.IEvent || executing is MessagingApi.ICommand);
        }
    }

    class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IReadOnlyList<MessagingApi.IMessage> executingMessages, MessagingApi.IMessage message)
        {
            if(message is MessagingApi.IQuery) return true;

            return executingMessages.None(executing => executing is MessagingApi.IEvent || executing is MessagingApi.ICommand);
        }
    }
}
