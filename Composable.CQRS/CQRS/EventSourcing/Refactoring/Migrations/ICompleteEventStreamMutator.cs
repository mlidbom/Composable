using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.Refactoring.Migrations
{
    interface ICompleteEventStreamMutator
    {
        IEnumerable<AggregateRootEvent> Mutate(IEnumerable<AggregateRootEvent> eventStream);
    }
}