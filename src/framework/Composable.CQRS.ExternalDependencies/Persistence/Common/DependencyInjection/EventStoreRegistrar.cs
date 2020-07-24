using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Refactoring.Naming;
using Composable.Serialization;

namespace Composable.Persistence.Common.DependencyInjection
{
    public static class EventStoreRegistrar
    {
        static readonly IEventMigration[] EmptyMigrationsArray = Array.Empty<IEventMigration>();

        public static EventStoreRegistrationBuilder RegisterEventStore(this IEndpointBuilder @this) => @this.RegisterEventStore(EmptyMigrationsArray);
        public static EventStoreRegistrationBuilder RegisterEventStore(this IEndpointBuilder @this, IReadOnlyList<IEventMigration> migrations)
        {
            @this.Container.RegisterEventStore(@this.Configuration.ConnectionStringName, migrations);
            return new EventStoreRegistrationBuilder(@this.RegisterHandlers);
        }

        public static void RegisterEventStore(this IDependencyInjectionContainer @this, string connectionName) =>
            @this.RegisterEventStore(connectionName, EmptyMigrationsArray);

        public static void RegisterEventStore(this IDependencyInjectionContainer @this,
                                              string connectionName,
                                              IReadOnlyList<IEventMigration> migrations) =>
            @this.RegisterEventStoreForFlexibleTesting(connectionName, () => migrations);

        internal static void RegisterEventStoreForFlexibleTesting(this IDependencyInjectionContainer @this,
                                                                  string connectionName,
                                                                  Func<IReadOnlyList<IEventMigration>> migrations)
        {
            Contract.ArgumentNotNullEmptyOrWhitespace(connectionName, nameof(connectionName));

            @this.Register(
                Singleton.For<IAggregateTypeValidator>()
                         .CreatedBy((ITypeMapper typeMapper) => new AggregateTypeValidator(typeMapper)),
                Singleton.For<IEventStoreSerializer>()
                         .CreatedBy((ITypeMapper typeMapper) => new EventStoreSerializer(typeMapper)),
                Singleton.For<EventCache>()
                         .CreatedBy(() => new EventCache()),
                Scoped.For<IEventStore>()
                      .CreatedBy((IEventStorePersistenceLayer persistenceLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache) =>
                                     new Persistence.EventStore.EventStore(persistenceLayer, typeMapper, serializer, cache, migrations())),
                Scoped.For<IEventStoreUpdater, IEventStoreReader>()
                      .CreatedBy((IEventStoreEventPublisher eventPublisher, IEventStore eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                                     new EventStoreUpdater(eventPublisher, eventStore, timeSource, aggregateTypeValidator)));
        }
    }

    public class EventStoreRegistrationBuilder
    {
        readonly MessageHandlerRegistrarWithDependencyInjectionSupport _handlerRegistrar;
        internal EventStoreRegistrationBuilder(MessageHandlerRegistrarWithDependencyInjectionSupport handlerRegistrar) => _handlerRegistrar = handlerRegistrar;

        public EventStoreRegistrationBuilder HandleAggregate<TAggregate, TEvent>()
            where TAggregate : IEventStored<TEvent>
            where TEvent : IAggregateEvent
        {
            EventStoreApi.RegisterHandlersForAggregate<TAggregate, TEvent>(_handlerRegistrar);
            return this;
        }
    }
}
