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

            return busState.InflightMessages.None(queued => queued.Message is IEvent || queued.Message is ICommand);
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

            return busState.LocallyExecutingMessages.None(executing => executing.Message is IEvent || executing.Message is ICommand);
        }
    }
}
