using System;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Composable.Persistence.EventStore
{
    public partial class EventStoreApi
    {
        public Query Queries => new Query();
        public Command Commands => new Command();

        public partial class Query
        {
            public AggregateLink<TAggregate> GetForUpdate<TAggregate>(Guid id) where TAggregate : IEventStored =>
                new AggregateLink<TAggregate>(id);

            public GetReadonlyCopyOfAggregate<TAggregate> GetReadOnlyCopy<TAggregate>(Guid id) where TAggregate : IEventStored =>
                new GetReadonlyCopyOfAggregate<TAggregate>(id);

            public GetReadonlyCopyOfAggregateVersion<TAggregate> GetReadOnlyCopyOfVersion<TAggregate>(Guid id, int version) where TAggregate : IEventStored =>
                new GetReadonlyCopyOfAggregateVersion<TAggregate>(id, version);

            public GetAggregateHistory<TEvent> GetHistory<TEvent>(Guid id) where TEvent : IAggregateEvent =>
                new GetAggregateHistory<TEvent>(id);
        }

        public partial class Command
        {
            public SaveAggregate<TAggregate> Save<TAggregate>(TAggregate account) where TAggregate : IEventStored => new SaveAggregate<TAggregate>(account);
        }
    }
}
