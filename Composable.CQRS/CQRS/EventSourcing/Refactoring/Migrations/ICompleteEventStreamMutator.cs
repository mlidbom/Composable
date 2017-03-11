using System.Collections.Generic;
using Composable.CQRS.EventSourcing;

namespace Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations
{
    interface ICompleteEventStreamMutator
    {
        IEnumerable<AggregateRootEvent> Mutate(IEnumerable<AggregateRootEvent> eventStream);
    }
}