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
    }
}