using System.Runtime.Serialization;
using Composable.CQRS.EventSourcing;
using NServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public class CatchSerializationErrors : IHandleMessages<IMessage>
    {
        private readonly IBus _bus;

        public CatchSerializationErrors(IBus bus)
        {
            _bus = bus;
        }

        public void Handle(IMessage message)
        {
            if (message.GetType() == typeof (IMessage) || message.GetType() == typeof (AggregateRootEvent))
            {
                throw new SerializationException("Message failed to serialize correctly");
            }
        }
    }
}