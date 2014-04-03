using System;

namespace Composable.CQRS
{
    public interface IAggregateRepository<TAggregate>
    {
        TAggregate Get(Guid id);
        void Add(TAggregate aggregate);
        TAggregate GetVersion(Guid aggregateRootId, int version);
    }
}
