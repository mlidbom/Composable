using System;

namespace Composable.CQRS.CQRS
{
    public interface IAggregateRepository<TAggregate>
    {
        // ReSharper disable once UnusedMember.Global todo: write test
        TAggregate Get(Guid id);
        void Add(TAggregate aggregate);
        TAggregate GetVersion(Guid aggregateRootId, int version);
    }
}