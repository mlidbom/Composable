using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public interface IEventModifier
    {
        void Replace(IEnumerable<AggregateRootEvent> events);
        void InsertBefore(IEnumerable<AggregateRootEvent> insert);
        //void InsertAfter(IEnumerable<IAggregateRootEvent> events); //Will not support guaranteeing that the migration is stable(Does not recursively change the stream again and again.) and will therefore not be supported
    }
}