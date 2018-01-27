using System;
using Composable.Messaging;

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

            public class AggregateLink<TEntity> : LocalQuery<TEntity>
            {
                public AggregateLink() {}
                public AggregateLink(Guid id) => Id = id;
                public Guid Id { get; set; }
                public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
            }

            public class GetReadonlyCopyOfAggregate<TEntity> : LocalQuery<TEntity>
            {
                public GetReadonlyCopyOfAggregate() {}
                public GetReadonlyCopyOfAggregate(Guid id) => Id = id;
                public Guid Id { get; set; }
                public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
            }

            public class GetReadonlyCopyOfAggregateVersion<TEntity> : LocalQuery<TEntity>
            {
                public GetReadonlyCopyOfAggregateVersion() {}
                public GetReadonlyCopyOfAggregateVersion(Guid id, int version)
                {
                    Id = id;
                    Version = version;
                }

                public Guid Id { get; set; }
                public int Version { get; set; }
                public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
            }
        }

        public class Command
        {
            public SaveAggregate<TAggregate> Save<TAggregate>(TAggregate account) => new SaveAggregate<TAggregate>(account);

            public class SaveAggregate<TEntity> : ExactlyOnceCommand
            {
                public SaveAggregate(TEntity entity) => Entity = entity;
                public TEntity Entity { get; }
            }
        }
    }
}
