using System.Runtime.Serialization;
using Composable.CQRS.EventSourcing;
using Composable.System;
using JetBrains.Annotations;
using NServiceBus;
using NServiceBus.MessageMutator;

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public class CatchSerializationErrorsMessageInspector : IMutateIncomingMessages
    {
        //todo: Try to check via the bus that the message actually implements all the interfaces that it should instead of this simplistic check for errors.
        public object MutateIncoming(object message)
        {
            if(message is IAggregateRootEvent && !message.IsInstanceOf<AggregateRootEvent>())
            {
                throw new SerializationException("Message failed to serialize correctly");
            }

            if (message.GetType() == typeof(IMessage) || message.GetType() == typeof(AggregateRootEvent))
            {
                throw new SerializationException("Message failed to serialize correctly");
            }
            return message;
        }
    }
}