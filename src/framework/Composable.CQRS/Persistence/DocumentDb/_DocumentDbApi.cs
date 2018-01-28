using System;
using Composable.DDD;
using Composable.Functional;
using Composable.Messaging;
// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.
// ReSharper disable UnusedTypeParameter it is vital for correct routing in the bus when more than one document type is registered in the document db.

namespace Composable.Persistence.DocumentDb

{
    public class DocumentDbApi
    {
        public Query Queries => new Query();
        public Command Commands => new Command();

        public class Query
        {
            public TryGetDocument<TDocument> TryGet<TDocument>(Guid id) where TDocument : IHasPersistentIdentity<Guid> => new TryGetDocument<TDocument>(id.ToString());

            public TryGetDocument<TDocument> TryGet<TDocument>(string id) => new TryGetDocument<TDocument>(id);

            public GetDocumentForUpdate<TDocument> GetForUpdate<TDocument>(Guid id) => new GetDocumentForUpdate<TDocument>(id);

            public GetReadonlyCopyOfDocument<TDocument> GetReadOnlyCopy<TDocument>(Guid id) => new GetReadonlyCopyOfDocument<TDocument>(id);

            public class GetDocumentForUpdate<TEntity> : BusApi.Local.Queries.Query<TEntity>
            {
                internal GetDocumentForUpdate(Guid id) => Id = id;
                internal Guid Id { get; private set; }
            }

            public class TryGetDocument<TEntity> : BusApi.Local.Queries.Query<Option<TEntity>>
            {
                internal TryGetDocument(string id) => Id = id;
                internal string Id { get; private set; }
            }

            public class GetReadonlyCopyOfDocument<TEntity> : BusApi.Local.Queries.Query<TEntity>
            {
                internal GetReadonlyCopyOfDocument(Guid id) => Id = id;
                internal Guid Id { get; private set; }
            }
        }

        public class Command
        {
            public SaveDocument<TDocument> Save<TDocument>(string key, TDocument account) => new SaveDocument<TDocument>(key, account);

            public SaveDocument<TDocument> Save<TDocument>(TDocument account) where TDocument : IHasPersistentIdentity<Guid> => new SaveDocument<TDocument>(account.Id.ToString(), account);

            public DeleteDocument<TDocument> Delete<TDocument>(string key) => new DeleteDocument<TDocument>(key);

            public class DeleteDocument<TEntity> : BusApi.Local.Commands.Command
            {
                internal DeleteDocument(string key) => Key = key;
                internal string Key { get; }
            }

            public class SaveDocument<TEntity> : BusApi.Local.Commands.Command
            {
                public SaveDocument(string key, TEntity entity)
                {
                    Key = key;
                    Entity = entity;
                }

                internal string Key { get; }
                internal TEntity Entity { get; }
            }
        }
    }
}
