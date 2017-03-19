using System.Collections.Generic;
using Composable.Persistence.EventSourcing;

namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    interface ICompleteEventStreamMutator
    {
        IEnumerable<AggregateRootEvent> Mutate(IEnumerable<AggregateRootEvent> eventStream);
    }
}