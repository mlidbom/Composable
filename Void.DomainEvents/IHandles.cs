namespace Void.DomainEvents
{
    public interface IHandles<in TEvent> where TEvent : IDomainEvent
    {
        void Handle(TEvent args); 
    }
}