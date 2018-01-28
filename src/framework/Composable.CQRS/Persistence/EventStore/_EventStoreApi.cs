using System;
using System.Collections.Generic;
using Composable.Messaging;
using Newtonsoft.Json;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Composable.Persistence.EventStore
{
    public class EventStoreApi
    {
        public Query Queries => new Query();
        public Command Commands => new Command();

        public class Query
        {
            public AggregateLink<TAggregate> GetForUpdate<TAggregate>(Guid id) => new AggregateLink<TAggregate>(id);

            public GetReadonlyCopyOfAggregate<TAggregate> GetReadOnlyCopy<TAggregate>(Guid id) => new GetReadonlyCopyOfAggregate<TAggregate>(id);

            public GetReadonlyCopyOfAggregateVersion<TAggregate> GetReadOnlyCopyOfVersion<TAggregate>(Guid id, int version) => new GetReadonlyCopyOfAggregateVersion<TAggregate>(id, version);

            public GetAggregateHistory<TEvent> GetHistory<TEvent>(Guid id) where TEvent : IAggregateEvent => new GetAggregateHistory<TEvent>(id);

            public class AggregateLink<TEntity> : BusApi.Local.Queries.Query<TEntity>
            {
                [JsonConstructor]internal AggregateLink(Guid id) => Id = id;
                public Guid Id { get; }
            }

            public class GetAggregateHistory<TEvent> : BusApi.Local.Queries.Query<IEnumerable<TEvent>> where TEvent : IAggregateEvent
            {
                [JsonConstructor]internal GetAggregateHistory(Guid id) => Id = id;
                public Guid Id { get; }
            }

            public class GetReadonlyCopyOfAggregate<TEntity> : BusApi.Local.Queries.Query<TEntity>
            {
                internal GetReadonlyCopyOfAggregate(Guid id) => Id = id;
                public Guid Id { get; }
            }

            public class GetReadonlyCopyOfAggregateVersion<TEntity> : BusApi.Local.Queries.Query<TEntity>
            {
                [JsonConstructor]internal GetReadonlyCopyOfAggregateVersion(Guid id, int version)
                {
                    Id = id;
                    Version = version;
                }

                public Guid Id { get; }
                public int Version { get; }
            }
        }

        public class Command
        {
            public SaveAggregate<TAggregate> Save<TAggregate>(TAggregate account) => new SaveAggregate<TAggregate>(account);

            public class SaveAggregate<TEntity> : BusApi.Local.Commands.Command
            {
                [JsonConstructor] internal SaveAggregate(TEntity entity) => Entity = entity;
                internal TEntity Entity { get; }
            }
        }
    }
}
