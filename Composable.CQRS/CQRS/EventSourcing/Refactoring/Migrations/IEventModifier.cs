using Composable.CQRS.EventSourcing;

namespace Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations
{
    public interface IEventModifier
    {
        void Replace(params AggregateRootEvent[] events);
        void InsertBefore(params AggregateRootEvent[] insert);
    }
}