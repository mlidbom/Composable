using System;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing;

namespace Composable.CQRS.CQRS
{
    public class AggregateRepository<TAggregate, TBaseEventClass, TBaseEventInterface> : IAggregateRepository<TAggregate>
        where TAggregate : AggregateRoot<TAggregate, TBaseEventClass, TBaseEventInterface>, IEventStored
        where TBaseEventClass : AggregateRootEvent, TBaseEventInterface
        where TBaseEventInterface : class, IAggregateRootEvent
    {
        readonly IEventStoreSession _aggregates;

        protected AggregateRepository(IEventStoreSession aggregates) => _aggregates = aggregates;

        public virtual TAggregate Get(Guid id) => _aggregates.Get<TAggregate>(id);

        public virtual void Add(TAggregate aggregate)
        {
            _aggregates.Save(aggregate);
        }

        public virtual TAggregate GetVersion(Guid aggregateRootId, int version) => _aggregates.LoadSpecificVersion<TAggregate>(aggregateRootId, version);
    }
}
