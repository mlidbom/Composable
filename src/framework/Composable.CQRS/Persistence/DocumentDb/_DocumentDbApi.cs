using System;
using Composable.DDD;
using Composable.Messaging;
// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Composable.Persistence.DocumentDb

{
    public class DocumentDbApi
    {
        public Query Queries => new Query();
        public Command Commands => new Command();

        public class Query
        {
            public TryGetDocument<TDocument> TryGet<TDocument>(Guid id) => new TryGetDocument<TDocument>(id);

            public DocumentLink<TDocument> GetForUpdate<TDocument>(Guid id) => new DocumentLink<TDocument>(id);

            public GetReadonlyCopyOfDocument<TDocument> GetReadOnlyCopy<TDocument>(Guid id) => new GetReadonlyCopyOfDocument<TDocument>(id);

            public class DocumentLink<TEntity> : ExactlyOnceMessage, MessagingApi.IQuery<TEntity>
            {
                internal DocumentLink() {}
                public DocumentLink(Guid id) => Id = id;
                public Guid Id { get; set; }
                public DocumentLink<TEntity> WithId(Guid id) => new DocumentLink<TEntity> {Id = id};
            }

            public class TryGetDocument<TEntity> : ExactlyOnceMessage, MessagingApi.IQuery<TEntity>
            {
                internal TryGetDocument() {}
                public TryGetDocument(Guid id) => Id = id;
                public Guid Id { get; set; }
                public DocumentLink<TEntity> WithId(Guid id) => new DocumentLink<TEntity> {Id = id};
            }

            public class GetReadonlyCopyOfDocument<TEntity> : ExactlyOnceMessage, MessagingApi.IQuery<TEntity>
            {
                public GetReadonlyCopyOfDocument() {}
                public GetReadonlyCopyOfDocument(Guid id) => Id = id;
                public Guid Id { get; set; }
                public DocumentLink<TEntity> WithId(Guid id) => new DocumentLink<TEntity> {Id = id};
            }

            public class GetReadonlyCopyOfDocumentVersion<TEntity> : ExactlyOnceMessage, MessagingApi.IQuery<TEntity>
            {
                public GetReadonlyCopyOfDocumentVersion() {}
                public GetReadonlyCopyOfDocumentVersion(Guid id, int version)
                {
                    Id = id;
                    Version = version;
                }

                public Guid Id { get; set; }
                public int Version { get; set; }
                public DocumentLink<TEntity> WithId(Guid id) => new DocumentLink<TEntity> {Id = id};
            }
        }

        public class Command
        {
            public SaveDocument<TDocument> Save<TDocument>(string key, TDocument account) => new SaveDocument<TDocument>(key, account);

            public SaveDocument<TDocument> Save<TDocument>(TDocument account) where TDocument : IHasPersistentIdentity<Guid> => new SaveDocument<TDocument>(account.Id.ToString(), account);

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
        }
    }
}
