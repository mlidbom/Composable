namespace Composable.CQRS.EventSourcing
{
    public interface IEventStore
    {
        IEventStoreSession OpenSession();
    }
}