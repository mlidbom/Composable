using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.Refactoring.Migrations
{
    internal interface ICompleteEventStreamMutator
    {
        IEnumerable<AggregateRootEvent> Mutate(IEnumerable<AggregateRootEvent> eventStream);
    }
}