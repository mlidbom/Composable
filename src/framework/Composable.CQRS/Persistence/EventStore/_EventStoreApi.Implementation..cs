using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Newtonsoft.Json;

namespace Composable.Persistence.EventStore
{
    public partial class EventStoreApi
    {
        public partial class QueryApi
        {
            public class AggregateLink<TAggregate> : MessageTypes.StrictlyLocal.Queries.Query<AggregateLink<TAggregate>, TAggregate> where TAggregate : IEventStored
            {
                [JsonConstructor] internal AggregateLink(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (AggregateLink<TAggregate> query, IEventStoreUpdater updater) => updater.Get<TAggregate>(query.Id));
            }

            public class GetAggregateHistory<TEvent> : MessageTypes.StrictlyLocal.Queries.Query<GetAggregateHistory<TEvent>, IEnumerable<TEvent>> where TEvent : IAggregateEvent
            {
                [JsonConstructor] internal GetAggregateHistory(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetAggregateHistory<TEvent> query, IEventStoreReader reader) => reader.GetHistory(query.Id).Cast<TEvent>());
            }

            public class GetReadonlyCopyOfAggregate<TAggregate> : MessageTypes.StrictlyLocal.Queries.Query<GetReadonlyCopyOfAggregate<TAggregate>, TAggregate> where TAggregate : IEventStored
            {
                [JsonConstructor] internal GetReadonlyCopyOfAggregate(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetReadonlyCopyOfAggregate<TAggregate> query, IEventStoreReader reader) => reader.GetReadonlyCopy<TAggregate>(query.Id));
            }

            public class GetReadonlyCopyOfAggregateVersion<TAggregate> : MessageTypes.StrictlyLocal.Queries.Query<GetReadonlyCopyOfAggregateVersion<TAggregate>, TAggregate> where TAggregate : IEventStored
            {
                [JsonConstructor] internal GetReadonlyCopyOfAggregateVersion(Guid id, int version)
                {
                    Id = id;
                    Version = version;
                }

                [JsonProperty] Guid Id { get; }
                [JsonProperty] int Version { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetReadonlyCopyOfAggregateVersion<TAggregate> query, IEventStoreReader reader) => reader.GetReadonlyCopyOfVersion<TAggregate>(query.Id, query.Version));
            }
        }

        public partial class Command
        {
            public class SaveAggregate<TAggregate> : MessageTypes.StrictlyLocal.Commands.Command
                where TAggregate : IEventStored
            {
                [JsonConstructor] internal SaveAggregate(TAggregate entity) => Entity = entity;
                TAggregate Entity { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                    (SaveAggregate<TAggregate> command, IEventStoreUpdater updater) => updater.Save(command.Entity));
            }
        }

        internal static void RegisterHandlersForAggregate<TAggregate, TEvent>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            where TAggregate : IEventStored<TEvent>
            where TEvent : IAggregateEvent
        {
            Command.SaveAggregate<TAggregate>.RegisterHandler(registrar);
            QueryApi.AggregateLink<TAggregate>.RegisterHandler(registrar);
            QueryApi.GetReadonlyCopyOfAggregate<TAggregate>.RegisterHandler(registrar);
            QueryApi.GetReadonlyCopyOfAggregateVersion<TAggregate>.RegisterHandler(registrar);
            QueryApi.GetAggregateHistory<TEvent>.RegisterHandler(registrar);
        }

        public static void MapTypes(ITypeMappingRegistar typeMapper)
        {
            typeMapper
               .MapTypeAndStandardCollectionTypes<AggregateEvent>("E8BA2E11-317C-416B-A68A-393CB6E5551B")
               .MapTypeAndStandardCollectionTypes<IAggregateEvent>("4634AF0A-E634-4970-8BF9-AAC0FDBD1255")
               .MapStandardCollectionTypes<IEventStored>("8857D0EB-9F41-414F-A12C-E8D5DE94AF3E");
        }
    }
}
