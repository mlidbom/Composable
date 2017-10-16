namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    public interface IEventModifier
    {
        void Replace(params DomainEvent[] events);
        void InsertBefore(params DomainEvent[] insert);
    }
}