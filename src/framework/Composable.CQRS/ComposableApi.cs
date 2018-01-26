using System;
using Composable.Messaging;

namespace Composable
{
    public class ComposableApi
    {
        public static class EventStoreManaging<TAggregate>
        {
            public static PersistEntityCommand<TAggregate> Save(TAggregate account) => new PersistEntityCommand<TAggregate>(account);

            public static AggregateLink<TAggregate> GetForUpdate(Guid id) => new AggregateLink<TAggregate>(id);

            public static GetReadonlyCopyOfAggregate<TAggregate> GetReadOnlyCopy(Guid id) => new GetReadonlyCopyOfAggregate<TAggregate>(id);

            public static GetReadonlyCopyOfAggregateVersion<TAggregate> GetReadOnlyCopyOfVersion(Guid id, int version) => new GetReadonlyCopyOfAggregateVersion<TAggregate>(id, version);
        }
    }
}
