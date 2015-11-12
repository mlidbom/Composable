using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    internal interface ICompleteEventStreamMutator
    {
        IEnumerable<IAggregateRootEvent> Mutate(IEnumerable<IAggregateRootEvent> eventStream);
    }
}