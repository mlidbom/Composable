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

            foreach(var inflightMessage in busState.InflightMessages)
            {
                if(inflightMessage.Message is IEvent || inflightMessage.Message is ICommand)
                {
                    return false;
                }
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

            foreach(var executingMessage in locallyExecutingMessages)
            {
                if(executingMessage is IEvent || executingMessage is ICommand)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
