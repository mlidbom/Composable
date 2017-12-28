using System;
using Composable.Messaging;
using Composable.Persistence.EventStore.AggregateRoots;

namespace Composable.Persistence.EventStore
{
    public class AggregateRepository<TAggregate, TBaseEventClass, TBaseEventInterface> : IAggregateRepository<TAggregate>
        where TAggregate : AggregateRoot<TAggregate, TBaseEventClass, TBaseEventInterface>, IEventStored
        where TBaseEventClass : DomainEvent, TBaseEventInterface
        where TBaseEventInterface : class, IDomainEvent
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

        //todo: readonly copy should throw exception if trying to publish events.
        public TAggregate GetReadonlyCopy(Guid aggregateRootId) => GetReadonlyCopyOfVersion(aggregateRootId, int.MaxValue);

        public virtual TAggregate GetReadonlyCopyOfVersion(Guid aggregateRootId, int version) => _reader.LoadSpecificVersion<TAggregate>(aggregateRootId, version);
    }
}
