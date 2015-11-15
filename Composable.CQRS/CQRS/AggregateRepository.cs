using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.System;

namespace Composable.CQRS
{
    public class AggregateRepository<TAggregate, TBaseEventClass, TBaseEventInterface> : IAggregateRepository<TAggregate>
        where TAggregate : TempAggregateRootWithTimeSourceSupport<TAggregate, TBaseEventClass, TBaseEventInterface>, IEventStored
        where TBaseEventClass : AggregateRootEvent, TBaseEventInterface
        where TBaseEventInterface : IAggregateRootEvent
    {
        protected readonly IEventStoreSession Aggregates;
        private readonly ITimeSource _timeSource;

        public AggregateRepository(IEventStoreSession aggregates, ITimeSource timeSource)
        {
            Aggregates = aggregates;
            _timeSource = timeSource;
        }

        public virtual TAggregate Get(Guid id)
        {
            return Aggregates.Get<TAggregate>(id).Do(aggregate => aggregate.TimeSource = _timeSource);
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
