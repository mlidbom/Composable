using System;
using Composable.DDD;
using Composable.Messaging;
using Composable.Messaging.Commands;

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


        public static class DocumentDB
        {
            public static SaveDocument<TDocument> Save<TDocument>(string key, TDocument account) => new SaveDocument<TDocument>(key, account);

            public static SaveDocument<TDocument> Save<TDocument>(TDocument account) where TDocument : IHasPersistentIdentity<Guid> => new SaveDocument<TDocument>(account.Id.ToString(), account);

            public static TryGetDocument<TDocument> TryGet<TDocument>(Guid id) => new TryGetDocument<TDocument>(id);

            public static DocumentLink<TDocument> GetForUpdate<TDocument>(Guid id) => new DocumentLink<TDocument>(id);

            public static GetReadonlyCopyOfDocument<TDocument> GetReadOnlyCopy<TDocument>(Guid id) => new GetReadonlyCopyOfDocument<TDocument>(id);

        }
    }

    namespace Messaging
    {
        public class DocumentLink<TEntity> : Message, IQuery<TEntity>
        {
            public DocumentLink() {}
            public DocumentLink(Guid id) => Id = id;
            public Guid Id { get; set; }
            public DocumentLink<TEntity> WithId(Guid id) => new DocumentLink<TEntity> {Id = id};
        }

        public class TryGetDocument<TEntity> : Message, IQuery<TEntity>
        {
            public TryGetDocument() {}
            public TryGetDocument(Guid id) => Id = id;
            public Guid Id { get; set; }
            public DocumentLink<TEntity> WithId(Guid id) => new DocumentLink<TEntity> {Id = id};
        }

        public class GetReadonlyCopyOfDocument<TEntity> : Message, IQuery<TEntity>
        {
            public GetReadonlyCopyOfDocument() {}
            public GetReadonlyCopyOfDocument(Guid id) => Id = id;
            public Guid Id { get; set; }
            public DocumentLink<TEntity> WithId(Guid id) => new DocumentLink<TEntity> {Id = id};
        }

        public class GetReadonlyCopyOfDocumentVersion<TEntity> : Message, IQuery<TEntity>
        {
            public GetReadonlyCopyOfDocumentVersion() {}
            public GetReadonlyCopyOfDocumentVersion(Guid id, int version)
            {
                Id = id;
                Version = version;
            }

            public Guid Id { get; set; }
            public int Version { get; set;}
            public DocumentLink<TEntity> WithId(Guid id) => new DocumentLink<TEntity> {Id = id};
        }

        public class SaveDocument<TEntity> : ExactlyOnceCommand
        {
            public SaveDocument(string key, TEntity entity)
            {
                Key = key;
                Entity = entity;
            }

            public string Key { get; }
            public TEntity Entity { get; }
        }

        public class AggregateLink<TEntity> : Message, IQuery<TEntity>
        {
            public AggregateLink() {}
            public AggregateLink(Guid id) => Id = id;
            public Guid Id { get; set; }
            public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
        }

        public class GetReadonlyCopyOfAggregate<TEntity> : Message, IQuery<TEntity>
        {
            public GetReadonlyCopyOfAggregate() {}
            public GetReadonlyCopyOfAggregate(Guid id) => Id = id;
            public Guid Id { get; set; }
            public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
        }

        public class GetReadonlyCopyOfAggregateVersion<TEntity> : Message, IQuery<TEntity>
        {
            public GetReadonlyCopyOfAggregateVersion() {}
            public GetReadonlyCopyOfAggregateVersion(Guid id, int version)
            {
                Id = id;
                Version = version;
            }

            public Guid Id { get; set; }
            public int Version { get; set;}
            public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
        }

        public class SaveAggregate<TEntity> : ExactlyOnceCommand
        {
            public SaveAggregate(TEntity entity) => Entity = entity;
            public TEntity Entity { get; }
        }
    }
}
