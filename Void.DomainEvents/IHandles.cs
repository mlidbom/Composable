namespace Void.DomainEvents
{
    public interface IHandles<TEvent>
    {
        void Handle(TEvent args); 
    }
}