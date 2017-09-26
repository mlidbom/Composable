using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IGlobalBusStateSnapshot busState, IReadOnlyList<IMessage> locallyExecutingMessages, IMessage message)
        {
            if(!(message is IQuery))
            {
                return true;
            }

            return busState.InflightMessages.None(IsEventOrCommand);
        }

        static bool IsEventOrCommand(IInflightMessage inflight) => inflight.Message is IEvent || inflight.Message is ICommand;
    }

    class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IGlobalBusStateSnapshot busState, IReadOnlyList<IMessage> locallyExecutingMessages, IMessage message)
        {
            if(message is IQuery)
            {
                return true;
            }

            return locallyExecutingMessages.None(IsEventOrCommand);
        }

        static bool IsEventOrCommand(IMessage inflight) => inflight is IEvent || inflight is ICommand;
    }
}
