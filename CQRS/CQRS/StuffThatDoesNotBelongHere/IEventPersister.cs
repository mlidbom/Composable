using Composable.DomainEvents;

namespace Composable.StuffThatDoesNotBelongHere
{
    public interface IEventPersister<in TEvent> where TEvent : IDomainEvent
    {
        void Persist(TEvent evt);
    }
}
