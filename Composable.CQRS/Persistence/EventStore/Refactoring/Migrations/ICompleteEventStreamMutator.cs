using System.Collections.Generic;

namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    interface ICompleteEventStreamMutator
    {
        IEnumerable<AggregateRootEvent> Mutate(IEnumerable<AggregateRootEvent> eventStream);
    }
}