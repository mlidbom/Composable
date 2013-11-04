using Composable.ServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public interface IDualDispatchBusRouter
    {
        bool SendToSynchronousBus(object message, SynchronousBus syncBus);
    }
}