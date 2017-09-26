using System.Collections.Generic;
using System.Linq;
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

            if(busState.InflightMessages.Select(dispatching => dispatching.Message).Any(dispatching => dispatching is ICommand || dispatching is IEvent))
            {
                return false;
            }

            return true;
        }
    }


    class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IGlobalBusStateSnapshot busState, IReadOnlyList<IMessage> locallyExecutingMessages, IMessage message)
        {
            if (message is IQuery)
            {
                return true;
            }

            return locallyExecutingMessages.OfType<IEvent>().None() && locallyExecutingMessages.OfType<ICommand>().None();
        }
    }
}
