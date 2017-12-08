using System.Linq;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IGlobalBusStateSnapshot busState, IQueuedMessageInformation queuedMessageInformation)
        {
            if(!(queuedMessageInformation.Message is IQuery))
            {
                return true;
            }

            if(busState.InFlightMessages.None(message => message is IEvent || message is IDomainCommand))
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

            return busState.MessagesQueuedForExecutionLocally.None(executing => executing.Message is IEvent || executing.Message is IDomainCommand);
        }
    }
}
