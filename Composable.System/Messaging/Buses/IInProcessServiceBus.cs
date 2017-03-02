using Composable.CQRS.EventSourcing;

namespace Composable.Messaging.Buses
{
    public interface IInProcessServiceBus
    {
        void Publish(IEvent anEvent);
        void Send(ICommand message);
    }
}
