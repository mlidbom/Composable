using System;

namespace Composable.Persistence.EventStore
{
    public interface IAggregateRepository<TAggregate>
    {
        // ReSharper disable once UnusedMember.Global todo: write test
        TAggregate Get(Guid id);
        void Add(TAggregate aggregate);
        TAggregate GetReadonlyCopy(Guid aggregateRootId);
        TAggregate GetReadonlyCopyOfVersion(Guid aggregateRootId, int version);
    }
}