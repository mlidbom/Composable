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

            if(busState.InFlightMessages.None(message => message.Type == TransportMessage.TransportMessageType.Event || message.Type == TransportMessage.TransportMessageType.Command))
            {
                return true;
            }

            return false;
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

            return busState.MessagesQueuedForExecutionLocally.None(executing => executing.Message is ITransactionalExactlyOnceDeliveryEvent || executing.Message is ITransactionalExactlyOnceDeliveryCommand);
        }
    }
}
