using System;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Composable.Persistence.EventStore
{
    public partial class EventStoreApi
    {
        public QueryApi Queries => new QueryApi();
        public Command Commands => new Command();

        public partial class QueryApi
        {
            public AggregateLink<TAggregate> GetForUpdate<TAggregate>(Guid id) where TAggregate : class, IEventStored =>
                new AggregateLink<TAggregate>(id);

            public GetReadonlyCopyOfAggregate<TAggregate> GetReadOnlyCopy<TAggregate>(Guid id) where TAggregate : class, IEventStored =>
                new GetReadonlyCopyOfAggregate<TAggregate>(id);

            public GetReadonlyCopyOfAggregateVersion<TAggregate> GetReadOnlyCopyOfVersion<TAggregate>(Guid id, int version) where TAggregate : class, IEventStored =>
                new GetReadonlyCopyOfAggregateVersion<TAggregate>(id, version);

            public GetAggregateHistory<TEvent> GetHistory<TEvent>(Guid id) where TEvent : IAggregateEvent =>
                new GetAggregateHistory<TEvent>(id);
        }

        public partial class Command
        {
            public SaveAggregate<TAggregate> Save<TAggregate>(TAggregate account) where TAggregate : class, IEventStored => new SaveAggregate<TAggregate>(account);
        }
    }
}
