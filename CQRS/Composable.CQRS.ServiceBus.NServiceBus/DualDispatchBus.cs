using System;
using Composable.ServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class DualDispatchBus : IServiceBus
    {
        private readonly SynchronousBus _local;
        private readonly NServiceBusServiceBus _realBus;

        public DualDispatchBus(SynchronousBus local, NServiceBusServiceBus realBus)
        {
            _local = local;
            _realBus = realBus;
        }

        public void Publish(object message)
        {
            _local.Publish(message);
            _realBus.Publish(message);
        }

        public void SendLocal(object message)
        {
            throw new NotImplementedException("No sane way to implement dual dispatch for sendlocal that I can come up with. Same registrations will be called sync and then via the bus...");
        }
    }
}
