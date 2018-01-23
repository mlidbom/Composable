using System.Collections.Generic;

namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    interface ICompleteEventStreamMutator
    {
        IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream);
    }
}