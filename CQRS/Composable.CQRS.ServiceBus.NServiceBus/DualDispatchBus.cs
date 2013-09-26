using System;
using Composable.ServiceBus;
using NServiceBus;

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

        public void Send(object message)
        {
            //There can only be one handler in a send scenario and we let the synchronous bus get the first pick.
            if(_local.Handles((IMessage)message))
            {
                _local.Send(message);
            }
            else
            {
                _realBus.Send(message);
            }
        }

        public void Reply(object message)
        {
            //I cannot come up with a way to decide whether to dispatch to the real bus or the sync bus except for just giving it to the sync bus if it wants it...
            if(_local.Handles((IMessage)message))
            {
                _local.Reply(message);
            }
            else
            {
                _realBus.Reply(message);
            }
        }
    }
}
