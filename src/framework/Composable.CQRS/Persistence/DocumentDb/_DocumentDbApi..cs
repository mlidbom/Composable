using System;
using Composable.DDD;
// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Composable.Persistence.DocumentDb
{
    public partial class DocumentDbApi
    {
        public Query Queries => new Query();
        public Command Commands => new Command();

        public partial class Query
        {
            public TryGetDocument<TDocument> TryGet<TDocument>(Guid id) where TDocument : IHasPersistentIdentity<Guid> => new TryGetDocument<TDocument>(id.ToString());

            public TryGetDocument<TDocument> TryGet<TDocument>(string id) => new TryGetDocument<TDocument>(id);

            public GetDocumentForUpdate<TDocument> GetForUpdate<TDocument>(Guid id) => new GetDocumentForUpdate<TDocument>(id);

            public GetReadonlyCopyOfDocument<TDocument> GetReadOnlyCopy<TDocument>(Guid id) => new GetReadonlyCopyOfDocument<TDocument>(id);
        }

        public partial class Command
        {
            public SaveDocument<TDocument> Save<TDocument>(string key, TDocument account) => new SaveDocument<TDocument>(key, account);

            public SaveDocument<TDocument> Save<TDocument>(TDocument account) where TDocument : IHasPersistentIdentity<Guid> => new SaveDocument<TDocument>(account.Id.ToString(), account);

            public DeleteDocument<TDocument> Delete<TDocument>(string key) => new DeleteDocument<TDocument>(key);
        }
    }
}
