using System;
using Composable.Functional;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Newtonsoft.Json;

namespace Composable.Persistence.DocumentDb
{
    public partial class DocumentDbApi
    {
        public partial class QueryApi
        {
            public class GetDocumentForUpdate<TDocument> : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<GetDocumentForUpdate<TDocument>, TDocument>
            {
                [JsonConstructor] internal GetDocumentForUpdate(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; set; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetDocumentForUpdate<TDocument> query, IDocumentDbUpdater updater) => updater.GetForUpdate<TDocument>(query.Id));
            }

            public class TryGetDocument<TDocument> : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<TryGetDocument<TDocument>, Option<TDocument>>
            {
                [JsonConstructor] internal TryGetDocument(string id) => Id = id;
                [JsonProperty] string Id { get; set; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (TryGetDocument<TDocument> query, IDocumentDbReader updater) => updater.TryGet<TDocument>(query.Id, out var document) ? Option.Some(document) : Option.None<TDocument>());
            }

            public class GetReadonlyCopyOfDocument<TDocument> : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<GetReadonlyCopyOfDocument<TDocument>, TDocument>
            {
                [JsonConstructor] internal GetReadonlyCopyOfDocument(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; set; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetReadonlyCopyOfDocument<TDocument> query, IDocumentDbReader reader) => reader.Get<TDocument>(query.Id));
            }
        }

        public partial class Command
        {
            public class DeleteDocument<TDocument> : MessageTypes.StrictlyLocal.Commands.StrictlyLocalCommand
            {
                internal DeleteDocument(string key) => Key = key;
                string Key { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                    (DeleteDocument<TDocument> command, IDocumentDbUpdater updater) => updater.Delete<TDocument>(command.Key));
            }

            public class SaveDocument<TDocument> : MessageTypes.StrictlyLocal.Commands.StrictlyLocalCommand
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
            QueryApi.TryGetDocument<TDocument>.RegisterHandler(registrar);
            QueryApi.GetReadonlyCopyOfDocument<TDocument>.RegisterHandler(registrar);
            QueryApi.GetDocumentForUpdate<TDocument>.RegisterHandler(registrar);
            Command.SaveDocument<TDocument>.RegisterHandler(registrar);
            Command.DeleteDocument<TDocument>.RegisterHandler(registrar);
        }

    }
}
