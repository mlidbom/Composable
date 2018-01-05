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

            var oldRuleOpinon = busState.InFlightMessages.None(message => message.Type == TransportMessage.TransportMessageType.Event || message.Type == TransportMessage.TransportMessageType.Command);
            var newOpinon = busState.MessagesQueuedForExecution.None(message => message.Message is IEvent || message.Message is ICommand);

            if(oldRuleOpinon != newOpinon)
            {
                if(busState.InFlightMessages.Any(@this => @this.Type == TransportMessage.TransportMessageType.Event))
                {
                    Console.WriteLine("Events in flight");
                }

                if(busState.InFlightMessages.Any(@this => @this.Type == TransportMessage.TransportMessageType.Command))
                {
                    Console.WriteLine("Commands in flight");
                }
            }

            return newOpinon;
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
