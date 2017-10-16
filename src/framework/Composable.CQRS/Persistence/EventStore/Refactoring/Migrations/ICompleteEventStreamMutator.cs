using System.Collections.Generic;

namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    interface ICompleteEventStreamMutator
    {
        IEnumerable<DomainEvent> Mutate(IEnumerable<DomainEvent> eventStream);
    }
}