using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    ///<summary>Implementations are responsible for mutating the events of one aggregate instance. Callers are required to call <see cref="Mutate"/> with each event in order and to end by calling <see cref="EndOfAggregate"/></summary>
    internal interface ISingleAggregateInstanceEventStreamMutator
    {
        IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event);
        IEnumerable<IAggregateRootEvent> EndOfAggregate();
    }
}
