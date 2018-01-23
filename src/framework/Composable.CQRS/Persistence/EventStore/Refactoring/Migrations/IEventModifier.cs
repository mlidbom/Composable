namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    public interface IEventModifier
    {
        void Replace(params AggregateEvent[] events);
        void InsertBefore(params AggregateEvent[] insert);
    }
}