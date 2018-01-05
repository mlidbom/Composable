using System;
using System.Linq;
using Composable.System.Linq;

namespace Composable.Messaging.Buses.Implementation
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IGlobalBusStateSnapshot busState, IQueuedMessageInformation queuedMessageInformation)
        {
            if(!(queuedMessageInformation.Message is IQuery))
            {
                return true;
            }

            return busState.MessagesQueuedForExecution.None(message => message.Message is IEvent || message.Message is ICommand);
        }
    }

    class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IGlobalBusStateSnapshot busState, IQueuedMessageInformation queuedMessageInformation)
        {
            if(queuedMessageInformation.Message is IQuery)
            {
                return true;
            }

            return busState.MessagesQueuedForExecutionLocally.None(executing => executing.Message is ITransactionalExactlyOnceDeliveryEvent
                                                                                || executing.Message is ITransactionalExactlyOnceDeliveryCommand);
        }
    }
}
