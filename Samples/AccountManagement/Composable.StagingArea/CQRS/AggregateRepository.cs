using System;
using Composable.CQRS.EventSourcing;

namespace Composable.CQRS
{
    public class AggregateRepository<TAggregate> : IAggregateRepository<TAggregate>
        where TAggregate : IEventStored
    {
        private readonly IEventStoreSession _aggregates;

        public AggregateRepository(IEventStoreSession aggregates)
        {
            _aggregates = aggregates;
        }

        public TAggregate Get(Guid id)
        {
            return _aggregates.Get<TAggregate>(id);
        }

        public void Add(TAggregate account)
        {
            _aggregates.Save(account);
        }

        public TAggregate GetVersion(Guid aggregateRootId, int version)
        {
            return _aggregates.LoadSpecificVersion<TAggregate>(aggregateRootId, version);
        }
    }
}
