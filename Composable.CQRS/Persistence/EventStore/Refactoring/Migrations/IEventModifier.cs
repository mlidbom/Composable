namespace Composable.CQRS.EventSourcing.Refactoring.Migrations
{
    public interface IEventModifier
    {
        void Replace(params AggregateRootEvent[] events);
        void InsertBefore(params AggregateRootEvent[] insert);
    }
}