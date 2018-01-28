using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Newtonsoft.Json;

namespace Composable.Persistence.EventStore
{
    public partial class EventStoreApi
    {
        public partial class Query
        {
            public class AggregateLink<TAggregate> : BusApi.Local.Queries.Query<TAggregate> where TAggregate : IEventStored
            {
                [JsonConstructor] internal AggregateLink(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (AggregateLink<TAggregate> query, IEventStoreUpdater updater) => updater.Get<TAggregate>(query.Id));
            }

            public class GetAggregateHistory<TEvent> : BusApi.Local.Queries.Query<IEnumerable<TEvent>> where TEvent : IAggregateEvent
            {
                [JsonConstructor] internal GetAggregateHistory(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetAggregateHistory<TEvent> query, IEventStoreReader reader) => reader.GetHistory(query.Id).Cast<TEvent>());
            }

            public class GetReadonlyCopyOfAggregate<TAggregate> : BusApi.Local.Queries.Query<TAggregate> where TAggregate : IEventStored
            {
                [JsonConstructor] internal GetReadonlyCopyOfAggregate(Guid id) => Id = id;
                [JsonProperty] Guid Id { get; }

                internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                    (GetReadonlyCopyOfAggregate<TAggregate> query, IEventStoreReader reader) => reader.GetReadonlyCopy<TAggregate>(query.Id));
            }

            public class GetReadonlyCopyOfAggregateVersion<TAggregate> : BusApi.Local.Queries.Query<TAggregate> where TAggregate : IEventStored
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
            public class SaveAggregate<TAggregate> : BusApi.Local.Commands.Command
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
            Query.AggregateLink<TAggregate>.RegisterHandler(registrar);
            Query.GetReadonlyCopyOfAggregate<TAggregate>.RegisterHandler(registrar);
            Query.GetReadonlyCopyOfAggregateVersion<TAggregate>.RegisterHandler(registrar);
            Query.GetAggregateHistory<TEvent>.RegisterHandler(registrar);
        }
    }
}
