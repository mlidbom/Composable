namespace Void.DomainEvents
{
    public interface IHandles<TEvent>  where TEvent : IDomainEvent
    {
        void Handle(TEvent args); 
    }
}