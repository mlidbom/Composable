using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class DefaultDualDispatchBusRouter : IDualDispatchBusRouter
    {
        public static readonly IDualDispatchBusRouter Instance = new DefaultDualDispatchBusRouter();
        public virtual bool SendToSynchronousBus(object message, SynchronousBus syncBus)
        {
            return syncBus.Handles((IMessage)message);
        }
    }
}