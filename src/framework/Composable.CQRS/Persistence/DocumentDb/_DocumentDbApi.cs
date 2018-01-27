using System;
using Composable.DDD;
using Composable.Functional;
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
            public TryGetDocument<TDocument> TryGet<TDocument>(Guid id) where TDocument : IHasPersistentIdentity<Guid> => new TryGetDocument<TDocument>(id);

            public GetDocumentForUpdate<TDocument> GetForUpdate<TDocument>(Guid id) => new GetDocumentForUpdate<TDocument>(id);

            public GetReadonlyCopyOfDocument<TDocument> GetReadOnlyCopy<TDocument>(Guid id) => new GetReadonlyCopyOfDocument<TDocument>(id);

            public class GetDocumentForUpdate<TEntity> : BusApi.Local.Queries.Query<TEntity>
            {
                internal GetDocumentForUpdate() {}
                public GetDocumentForUpdate(Guid id) => Id = id;
                public Guid Id { get; set; }
                public GetDocumentForUpdate<TEntity> WithId(Guid id) => new GetDocumentForUpdate<TEntity> {Id = id};
            }

            public class TryGetDocument<TEntity> : BusApi.Local.Queries.Query<Option<TEntity>>
            {
                internal TryGetDocument() {}
                public TryGetDocument(Guid id) => Id = id;
                public Guid Id { get; set; }
                public GetDocumentForUpdate<TEntity> WithId(Guid id) => new GetDocumentForUpdate<TEntity> {Id = id};
            }

            public class GetReadonlyCopyOfDocument<TEntity> : BusApi.Local.Queries.Query<TEntity>
            {
                public GetReadonlyCopyOfDocument() {}
                public GetReadonlyCopyOfDocument(Guid id) => Id = id;
                public Guid Id { get; set; }
                public GetDocumentForUpdate<TEntity> WithId(Guid id) => new GetDocumentForUpdate<TEntity> {Id = id};
            }

            public class GetReadonlyCopyOfDocumentVersion<TEntity> : BusApi.Local.Queries.Query<TEntity>
            {
                public GetReadonlyCopyOfDocumentVersion() {}
                public GetReadonlyCopyOfDocumentVersion(Guid id, int version)
                {
                    Id = id;
                    Version = version;
                }

                public Guid Id { get; set; }
                public int Version { get; set; }
                public GetDocumentForUpdate<TEntity> WithId(Guid id) => new GetDocumentForUpdate<TEntity> {Id = id};
            }
        }

        public class Command
        {
            public SaveDocument<TDocument> Save<TDocument>(string key, TDocument account) => new SaveDocument<TDocument>(key, account);

            public SaveDocument<TDocument> Save<TDocument>(TDocument account) where TDocument : IHasPersistentIdentity<Guid> => new SaveDocument<TDocument>(account.Id.ToString(), account);

            public class SaveDocument<TEntity> : BusApi.Local.Commands.Command
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
