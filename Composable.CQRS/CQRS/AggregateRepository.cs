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
        readonly IEventStoreSession Aggregates;

        protected AggregateRepository(IEventStoreSession aggregates)
        {
            Aggregates = aggregates;
        }

        public virtual TAggregate Get(Guid id)
        {
            return Aggregates.Get<TAggregate>(id);
        }

        public virtual void Add(TAggregate aggregate)
        {
            Aggregates.Save(aggregate);
        }

        public virtual TAggregate GetVersion(Guid aggregateRootId, int version)
        {
            return Aggregates.LoadSpecificVersion<TAggregate>(aggregateRootId, version);
        }
    }
}
