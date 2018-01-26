using System;
using Composable.Messaging;

namespace Composable
{
    public class ComposableApi
    {
        public static class EventStoreManaging<TAggregate>
        {
            public static SaveAggregate<TAggregate> Save(TAggregate account) => new SaveAggregate<TAggregate>(account);

            public static AggregateLink<TAggregate> GetForUpdate(Guid id) => new AggregateLink<TAggregate>(id);

            public static GetReadonlyCopyOfAggregate<TAggregate> GetReadOnlyCopy(Guid id) => new GetReadonlyCopyOfAggregate<TAggregate>(id);

            public static GetReadonlyCopyOfAggregateVersion<TAggregate> GetReadOnlyCopyOfVersion(Guid id, int version) => new GetReadonlyCopyOfAggregateVersion<TAggregate>(id, version);
        }
    }
}
