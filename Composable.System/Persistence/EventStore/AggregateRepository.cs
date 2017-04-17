using System;
using Composable.Persistence.EventStore.AggregateRoots;

namespace Composable.Persistence.EventStore
{
    public class AggregateRepository<TAggregate, TBaseEventClass, TBaseEventInterface> : IAggregateRepository<TAggregate>
        where TAggregate : AggregateRoot<TAggregate, TBaseEventClass, TBaseEventInterface>, IEventStored
        where TBaseEventClass : AggregateRootEvent, TBaseEventInterface
        where TBaseEventInterface : class, IAggregateRootEvent
    {
        readonly IEventStoreReader _reader;
        readonly IEventStoreUpdater _aggregates;

        protected AggregateRepository(IEventStoreUpdater aggregates, IEventStoreReader reader)
        {
            _aggregates = aggregates;
            _reader = reader;
        }

        public virtual TAggregate Get(Guid id) => _aggregates.Get<TAggregate>(id);

        public virtual void Add(TAggregate aggregate)
        {
            _aggregates.Save(aggregate);
        }

        public virtual TAggregate GetVersion(Guid aggregateRootId, int version) => _reader.LoadSpecificVersion<TAggregate>(aggregateRootId, version);
    }
}
