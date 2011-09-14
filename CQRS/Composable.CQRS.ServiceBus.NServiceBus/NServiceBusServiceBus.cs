using System;
using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class NServiceBusServiceBus : IServiceBus
    {
        private IBus _bus;

        public NServiceBusServiceBus(IBus bus)
        {
            _bus = bus;
        }

        public void Publish(Object message)
        {
            _bus.Publish((IMessage)message);
        }

        public void SendLocal(object message)
        {
            _bus.SendLocal((IMessage)message);
        }

        public void Send(object message)
        {
            _bus.Send((IMessage) message);
        }
    }
}