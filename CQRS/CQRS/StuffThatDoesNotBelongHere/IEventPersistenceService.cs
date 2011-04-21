using Composable.DomainEvents;

namespace Composable.StuffThatDoesNotBelongHere
{
    public interface IEventPersistenceService
    {
        void Persist<TEvent>(TEvent evt) where TEvent : IDomainEvent;
    }
}