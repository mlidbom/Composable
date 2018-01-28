using System;
using Composable.Functional;
using Composable.Messaging;
// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.
// ReSharper disable UnusedTypeParameter it is vital for correct routing in the bus when more than one document type is registered in the document db.

namespace Composable.Persistence.DocumentDb

{
    public partial class DocumentDbApi
    {
        public partial class Query
        {
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

        public partial class Command
        {
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
