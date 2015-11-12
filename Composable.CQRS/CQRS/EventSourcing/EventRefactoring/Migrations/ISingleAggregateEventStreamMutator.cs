using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal interface ISingleAggregateEventStreamMutator
    {
        IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event);
        IEnumerable<IAggregateRootEvent> EndOfAggregate();
    }
}
