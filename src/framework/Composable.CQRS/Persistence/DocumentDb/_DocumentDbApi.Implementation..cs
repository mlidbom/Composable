using System;
using Composable.Functional;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Newtonsoft.Json;

namespace Composable.Persistence.DocumentDb
{
    public partial class DocumentDbApi
    {
        public partial class Query
        {
            public class GetDocumentForUpdate<TDocument> : BusApi.Local.Queries.Query<TDocument>
            {
                [JsonConstructor] internal GetDocumentForUpdate(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; set; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetDocumentForUpdate<TDocument> query, IDocumentDbUpdater updater) => updater.GetForUpdate<TDocument>(query.Id));
            }

            public class TryGetDocument<TDocument> : BusApi.Local.Queries.Query<Option<TDocument>>
            {
                [JsonConstructor] internal TryGetDocument(string id) => Id = id;
                [JsonProperty] string Id { get; set; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (TryGetDocument<TDocument> query, IDocumentDbReader updater) => updater.TryGet<TDocument>(query.Id, out var document) ? Option.Some(document) : Option.None<TDocument>());
            }

            public class GetReadonlyCopyOfDocument<TDocument> : BusApi.Local.Queries.Query<TDocument>
            {
                [JsonConstructor] internal GetReadonlyCopyOfDocument(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; set; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetReadonlyCopyOfDocument<TDocument> query, IDocumentDbReader reader) => reader.Get<TDocument>(query.Id));
            }
        }

        public partial class Command
        {
            public class DeleteDocument<TDocument> : BusApi.Local.Commands.Command
            {
                internal DeleteDocument(string key) => Key = key;
                string Key { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                    (DeleteDocument<TDocument> command, IDocumentDbUpdater updater) => updater.Delete<TDocument>(command.Key));
            }

            public class SaveDocument<TDocument> : BusApi.Local.Commands.Command
            {
                internal SaveDocument(string key, TDocument entity)
                {
                    Key = key;
                    Entity = entity;
                }

                string Key { get; }
                TDocument Entity { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                    (DocumentDbApi.Command.SaveDocument<TDocument> command, IDocumentDbUpdater updater) => updater.Save(command.Key, command.Entity));
            }
        }

        internal static void HandleDocumentType<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            Query.TryGetDocument<TDocument>.RegisterHandler(registrar);
            Query.GetReadonlyCopyOfDocument<TDocument>.RegisterHandler(registrar);
            Query.GetDocumentForUpdate<TDocument>.RegisterHandler(registrar);
            Command.SaveDocument<TDocument>.RegisterHandler(registrar);
            Command.DeleteDocument<TDocument>.RegisterHandler(registrar);
        }

    }
}
