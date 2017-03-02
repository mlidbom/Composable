using Composable.CQRS.EventSourcing;

namespace Composable.Messaging
{
    public interface IEventListener<in TEvent> where TEvent : IEvent
    {
        void Handle(TEvent message);
    }
}
