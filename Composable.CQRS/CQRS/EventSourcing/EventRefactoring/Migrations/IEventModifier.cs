using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public interface IEventModifier
    {
        void Replace(IReadOnlyList<AggregateRootEvent> events);
        void InsertBefore(IReadOnlyList<AggregateRootEvent> insert);
    }
}